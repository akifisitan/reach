using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;

namespace Reach.Relay.Confluence;

public sealed class ConfluenceCommentRelay : ILocalRelay, IRemoteRelay
{
    private const string LocalToRemoteCursorName = "confluence.local_to_remote";
    private const string RemoteToLocalCursorName = "confluence.remote_to_local";

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private readonly IConfluenceHttpClientFactory _confluenceHttpClientFactory;
    private readonly ILogger<ConfluenceCommentRelay> _logger;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly ConfluenceOptions _options;
    private readonly IRelayCursorStorage _cursorStorage;

    public ConfluenceCommentRelay(
        IConfluenceHttpClientFactory confluenceHttpClientFactory,
        ILogger<ConfluenceCommentRelay> logger,
        IRelayPayloadSerializer payloadSerializer,
        IOptions<ConfluenceOptions> options,
        IRelayCursorStorage cursorStorage
    )
    {
        _confluenceHttpClientFactory = confluenceHttpClientFactory;
        _logger = logger;
        _payloadSerializer = payloadSerializer;
        _options = options.Value;
        _cursorStorage = cursorStorage;
    }

    public async Task<PostToRemoteResult> PostToRemote(
        RelayCommand value,
        CancellationToken cancellationToken
    )
    {
        var payload = _payloadSerializer.Serialize(value);

        _logger.ZLogInformation($"Sending payload from local to remote: {payload}");

        var base64Text = payload.EncodeBase64();

        await CreateComment(base64Text, _options.LocalToRemotePageId, cancellationToken)
            .ConfigureAwait(false);

        return new(new(value.Id));
    }

    public async Task<PostToLocalResult> PostToLocal(
        RelayCommandResult value,
        CancellationToken cancellationToken
    )
    {
        var payload = _payloadSerializer.Serialize(value);

        _logger.ZLogInformation($"Sending payload from remote to local: {payload}");

        var base64Text = payload.EncodeBase64();

        await CreateComment(base64Text, _options.RemoteToLocalPageId, cancellationToken)
            .ConfigureAwait(false);

        return new();
    }

    public IAsyncEnumerable<RelayCommand> PollFromLocal(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommand>(
            _options.LocalToRemotePageId,
            LocalToRemoteCursorName,
            cancellationToken
        );
    }

    public IAsyncEnumerable<RelayCommandResult> PollFromRemote(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommandResult>(
            _options.RemoteToLocalPageId,
            RemoteToLocalCursorName,
            cancellationToken
        );
    }

    // Assumptions
    // 1. The confluence comment page is append only. No comment deletions or updates. Only new comments
    // 2. The api returns comments sorted by id or creation date. This order does not change
    // 3. The comment id is the cursor: only comments with an id greater than the stored cursor are consumed
    private async IAsyncEnumerable<TOut> InnerPoll<TOut>(
        long pageId,
        string cursorName,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var previousCursor = await _cursorStorage.Retrieve(cursorName, cancellationToken);

        var lastSeenCommentId = previousCursor?.CurrentIndex ?? 0;

        var pollDelayMs = _options.PollDelayMs;

        var consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<ResultContent> newComments;

            try
            {
                newComments = await ReadNewCommentsFromPage(
                        pageId,
                        lastSeenCommentId,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;

                if (IsAuthFailure(ex))
                {
                    _logger.ZLogError(
                        ex,
                        $"Confluence authentication or authorization failed. Restart the application to reconfigure the credential"
                    );
                }
                else
                {
                    _logger.ZLogError(ex, $"An error occurred while polling Confluence comments");
                }

                // Exponential backoff: double the poll delay per consecutive failure, capped at MaxBackoff
                var backoff =
                    TimeSpan.FromMilliseconds(pollDelayMs)
                    * Math.Pow(2, Math.Min(consecutiveFailures - 1, 10));

                if (backoff > MaxBackoff)
                {
                    backoff = MaxBackoff;
                }

                await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);

                continue;
            }

            if (newComments.Count == 0)
            {
                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            foreach (var resultContent in newComments)
            {
                var commentId = long.Parse(resultContent.Id);

                if (commentId <= lastSeenCommentId)
                {
                    continue;
                }

                TOut command;

                try
                {
                    var base64Text = resultContent.Body.View.Value.StripHtml();

                    var jsonText = base64Text.DecodeBase64();

                    command = _payloadSerializer.Deserialize<TOut>(jsonText);

                    ArgumentNullException.ThrowIfNull(command);
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Value cannot be deserialized, skipping value");

                    lastSeenCommentId = Math.Max(lastSeenCommentId, commentId);

                    continue;
                }

                _logger.ZLogDebug($"Received value: {command.ToString()}");

                lastSeenCommentId = Math.Max(lastSeenCommentId, commentId);

                _logger.ZLogDebug($"Set {nameof(lastSeenCommentId)} to {lastSeenCommentId}");

                yield return command;
            }

            await _cursorStorage.Store(
                cursorName,
                new RelayCursor(lastSeenCommentId, DateTimeOffset.Now),
                cancellationToken
            );

            await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsAuthFailure(Exception ex)
    {
        return ex
            is HttpRequestException
            {
                StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            };
    }

    private async Task<CreateContentResponse?> CreateComment(
        string text,
        long pageId,
        CancellationToken cancellationToken
    )
    {
        var request = new CreateContentRequest(
            Type: "comment",
            Container: new CreateContentRequestContainer(Id: pageId, Type: "page"),
            Body: new CreateContentRequestBody(
                Storage: new CreateContentRequestBodyStorage(Value: text, Representation: "storage")
            )
        );

        using var httpClient = _confluenceHttpClientFactory.CreateClient();

        using var response = await httpClient
            .PostAsJsonAsync(
                "/confluence/rest/api/content",
                request,
                ConfluenceApiJsonSerializerContext.Default.CreateContentRequest,
                cancellationToken
            )
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response
            .Content.ReadFromJsonAsync(
                ConfluenceApiJsonSerializerContext.Default.CreateContentResponse,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    // Pages through the comment list (oldest first) and returns only comments
    // with an id greater than afterCommentId
    private async Task<List<ResultContent>> ReadNewCommentsFromPage(
        long pageId,
        long afterCommentId,
        CancellationToken cancellationToken
    )
    {
        using var httpClient = _confluenceHttpClientFactory.CreateClient();

        List<ResultContent> newComments = [];

        var start = 0;

        // TODO: this could hammer the relay no?
        while (true)
        {
            var batch = await ReadCommentsPage(httpClient, pageId, start, cancellationToken)
                .ConfigureAwait(false);

            foreach (var item in batch.Results)
            {
                if (long.Parse(item.Id) > afterCommentId)
                {
                    newComments.Add(item);
                }
            }

            if (batch.Size < batch.Limit || batch.Results.Count == 0)
            {
                break;
            }

            start += batch.Size;
        }

        return newComments;
    }

    private async Task<GetCommentsResponse> ReadCommentsPage(
        HttpClient httpClient,
        long pageId,
        int start,
        CancellationToken cancellationToken
    )
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query.Add("start", start.ToString());
        query.Add("limit", _options.Count.ToString());
        query.Add("location", "footer");
        query.Add("expand", "history,body.view");

        var uriBuilder = new UriBuilder(
            $"{httpClient.BaseAddress}confluence/rest/api/content/{pageId}/child/comment"
        )
        {
            Query = query.ToString(),
        };

        var uri = uriBuilder.Uri;

        using var responseMessage = await httpClient
            .GetAsync(uri, cancellationToken)
            .ConfigureAwait(false);

        var responseText = await responseMessage
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        responseMessage.EnsureSuccessStatusCode();

        var response = JsonSerializer.Deserialize(
            responseText,
            ConfluenceApiJsonSerializerContext.Default.GetCommentsResponse
        );

        ArgumentNullException.ThrowIfNull(response);

        return response;
    }
}
