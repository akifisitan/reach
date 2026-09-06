using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;

namespace Reach.Relay.Confluence;

// Assumptions
// 1. The confluence comment page is append only. No comment deletions or updates. Only new comments
// 2. The api returns comments sorted by id or creation date. This order does not change
// 3. The comment id is the cursor: only comments with an id greater than the stored cursor are consumed
public sealed class ConfluenceCommentRelay : ILocalRelay, IRemoteRelay
{
    private const string LocalToRemoteCursorName = "confluence.local_to_remote";
    private const string RemoteToLocalCursorName = "confluence.remote_to_local";

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private readonly IConfluenceHttpClientFactory _confluenceHttpClientFactory;
    private readonly ILogger<ConfluenceCommentRelay> _logger;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly IRelayTextEncoding _textEncoding;
    private readonly ConfluenceConfigurationStore _store;
    private readonly IRelayCursorStorage _cursorStorage;

    private ConfluenceOptions Options => _store.Current;

    public ConfluenceCommentRelay(
        IConfluenceHttpClientFactory confluenceHttpClientFactory,
        ILogger<ConfluenceCommentRelay> logger,
        IRelayPayloadSerializer payloadSerializer,
        IRelayTextEncoding textEncoding,
        ConfluenceConfigurationStore configurationStore,
        IRelayCursorStorage cursorStorage
    )
    {
        _confluenceHttpClientFactory = confluenceHttpClientFactory;
        _logger = logger;
        _payloadSerializer = payloadSerializer;
        _textEncoding = textEncoding;
        _store = configurationStore;
        _cursorStorage = cursorStorage;
    }

    public async Task<PostToRemoteResult> PostToRemote(
        RelayCommand value,
        CancellationToken cancellationToken
    )
    {
        var payload = _textEncoding.Encode(_payloadSerializer.Serialize(value));

        _logger.ZLogTrace($"Sending payload from local to remote: {payload}");

        await CreateComment(payload, Options.LocalToRemotePageId, cancellationToken)
            .ConfigureAwait(false);

        return new(new(value.Id));
    }

    public async Task<PostToLocalResult> PostToLocal(
        RelayCommandResult value,
        CancellationToken cancellationToken
    )
    {
        var payload = _textEncoding.Encode(_payloadSerializer.Serialize(value));

        _logger.ZLogTrace($"Sending payload from remote to local: {payload}");

        await CreateComment(payload, Options.RemoteToLocalPageId, cancellationToken)
            .ConfigureAwait(false);

        return new();
    }

    public IAsyncEnumerable<RelayDelivery<RelayCommand>> PollFromLocal(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommand>(
            Options.LocalToRemotePageId,
            GetCursorName(LocalToRemoteCursorName, Options.LocalToRemotePageId),
            cancellationToken
        );
    }

    public IAsyncEnumerable<RelayDelivery<RelayCommandResult>> PollFromRemote(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommandResult>(
            Options.RemoteToLocalPageId,
            GetCursorName(RemoteToLocalCursorName, Options.RemoteToLocalPageId),
            cancellationToken
        );
    }

    private static string GetCursorName(string direction, long pageId) => $"{direction}.{pageId}";

    private async IAsyncEnumerable<RelayDelivery<TOut>> InnerPoll<TOut>(
        long pageId,
        string cursorName,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var previousCursor = await _cursorStorage
            .Retrieve(cursorName, cancellationToken)
            .ConfigureAwait(false);

        var lastSeenCommentId = previousCursor?.CurrentIndex ?? 0;

        var pageOffset = previousCursor?.PageOffset ?? 0;

        var pollDelayMs = Options.PollDelayMs;

        var consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<ResultContent> newComments;

            try
            {
                (newComments, pageOffset) = await ReadNewCommentsFromPage(
                        pageId,
                        lastSeenCommentId,
                        pageOffset,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;

                if (ex.IsAuthError())
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
                    var text = resultContent.Body.View.Value.GetHtmlInnerText();

                    var payload = _textEncoding.Decode(text);

                    command = _payloadSerializer.Deserialize<TOut>(payload);

                    ArgumentNullException.ThrowIfNull(command);
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Value cannot be deserialized, skipping value");

                    lastSeenCommentId = Math.Max(lastSeenCommentId, commentId);

                    await _cursorStorage
                        .Store(
                            cursorName,
                            new RelayCursor(lastSeenCommentId, DateTimeOffset.Now, pageOffset),
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    continue;
                }

                _logger.ZLogTrace($"Received value: {command.ToString()}");

                var nextCommentId = Math.Max(lastSeenCommentId, commentId);
                var nextCursor = new RelayCursor(nextCommentId, DateTimeOffset.Now, pageOffset);

                yield return new(
                    command,
                    async ct =>
                    {
                        lastSeenCommentId = nextCommentId;

                        _logger.ZLogTrace(
                            $"Set {nameof(lastSeenCommentId)} to {lastSeenCommentId}"
                        );

                        await _cursorStorage
                            .Store(cursorName, nextCursor, ct)
                            .ConfigureAwait(false);
                    }
                );
            }

            await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);
        }
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

    // Pages through the comment list (oldest first), starting at the persisted page offset,
    // and returns only comments with an id greater than afterCommentId
    private async Task<(
        List<ResultContent> NewComments,
        long LastPageStart
    )> ReadNewCommentsFromPage(
        long pageId,
        long afterCommentId,
        long startOffset,
        CancellationToken cancellationToken
    )
    {
        using var httpClient = _confluenceHttpClientFactory.CreateClient();

        List<ResultContent> newComments = [];

        var start = startOffset;

        var driftChecked = false;

        while (true)
        {
            var batch = await ReadCommentsPage(httpClient, pageId, start, cancellationToken)
                .ConfigureAwait(false);

            // On resume from a stored offset, the cursor comment must still be within the
            // scanned range. If it is not, the append-only assumption was violated (e.g. a
            // comment was deleted), offsets have drifted and we restart from the beginning
            if (!driftChecked)
            {
                driftChecked = true;

                if (
                    startOffset > 0
                    && batch.Results.Count > 0
                    && batch.Results.Max(x => long.Parse(x.Id)) < afterCommentId
                )
                {
                    _logger.ZLogError(
                        $"Stored comment page offset has drifted, restarting pagination from the beginning"
                    );

                    start = 0;

                    continue;
                }
            }

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

        return (newComments, start);
    }

    private async Task<GetCommentsResponse> ReadCommentsPage(
        HttpClient httpClient,
        long pageId,
        long start,
        CancellationToken cancellationToken
    )
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query.Add("start", start.ToString());
        query.Add("limit", Options.BatchLimit.ToString());
        query.Add("location", "footer");
        query.Add("expand", "history,body.view");

        var uri = $"/confluence/rest/api/content/{pageId}/child/comment?{query}";

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
