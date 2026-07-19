using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using Polly;
using Reach.Relay.Abstractions;

namespace Reach.Relay.Confluence;

public sealed class ConfluenceCommentRelay : ILocalRelay, IRemoteRelay
{
    private readonly IConfluenceHttpClientFactory _confluenceHttpClientFactory;
    private readonly ILogger<ConfluenceCommentRelay> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly ConfluenceOptions _options;
    private readonly IRelayCursorStorage _cursorStorage;

    public ConfluenceCommentRelay(
        IConfluenceHttpClientFactory confluenceHttpClientFactory,
        ILogger<ConfluenceCommentRelay> logger,
        [FromKeyedServices(nameof(ConfluenceCommentRelay))] ResiliencePipeline resiliencePipeline,
        IRelayPayloadSerializer payloadSerializer,
        IOptions<ConfluenceOptions> options,
        IRelayCursorStorage cursorStorage
    )
    {
        _confluenceHttpClientFactory = confluenceHttpClientFactory;
        _logger = logger;
        _resiliencePipeline = resiliencePipeline;
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

        var response = await CreateComment(
                base64Text,
                _options.LocalToRemotePageId,
                cancellationToken
            )
            .ConfigureAwait(false);

        //response.Id confluence id

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
        return InnerPoll<RelayCommand>(options, _options.LocalToRemotePageId, cancellationToken);
    }

    public IAsyncEnumerable<RelayCommandResult> PollFromRemote(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommandResult>(
            options,
            _options.RemoteToLocalPageId,
            cancellationToken
        );
    }

    // Assumptions
    // 1. The confluence comment page is append only. No comment deletions or updates. Only new comments
    // 2. The api returns comments sorted by id or creation date. This order does not change
    // 3. We use the comment id for tracking previously consumed messages in this layer
    public async IAsyncEnumerable<TOut> InnerPoll<TOut>(
        PollingOptions? options,
        long pageId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var lastReceivedTime = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(20));
        var lastIndex = 0;

        var cursor =
            await _cursorStorage.Retrieve(cancellationToken)
            ?? new RelayCursor(lastIndex, lastReceivedTime);

        var pollDelayMs = _options.PollDelayMs;

        HashSet<string> processedEntries = [];

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GetCommentsResponse response;

            try
            {
                response = await ReadCommentsFromPage(
                        pageId,
                        _options.Count,
                        cursor,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred");

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            if (response.Results.Count == 0)
            {
                _logger.ZLogInformation($"No comments found");

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            foreach (var resultContent in response.Results)
            {
                var commentId = resultContent.Id;

                if (processedEntries.Contains(commentId))
                {
                    _logger.ZLogInformation($"Skipping already processed entry: {commentId}");

                    continue;
                }

                if (cursor.CreationTime > resultContent.History.CreatedDate)
                {
                    _logger.ZLogInformation($"Skipping comment created before cursor: {commentId}");

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

                    continue;
                }

                _logger.ZLogInformation($"Received value: {command.ToString()}");

                processedEntries.Add(commentId);

                yield return command;
            }

            var latestCreatedAt =
                response
                    .Results.LastOrDefault(x => x.History.CreatedDate >= cursor.CreationTime)
                    ?.History.CreatedDate
                ?? cursor.CreationTime;

            cursor = cursor with
            {
                CurrentIndex = cursor.CurrentIndex + response.Size,
                CreationTime = latestCreatedAt,
            };

            await _cursorStorage.Store(cursor, cancellationToken);

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

        var httpClient = _confluenceHttpClientFactory.CreateClient();

        var response = await httpClient
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

    private async Task<GetCommentsResponse> ReadCommentsFromPage(
        long pageId,
        int count,
        RelayCursor cursor,
        CancellationToken cancellationToken
    )
    {
        var httpClient = _confluenceHttpClientFactory.CreateClient();

        var query = HttpUtility.ParseQueryString(string.Empty);

        query.Add("start", cursor.CurrentIndex.ToString());
        query.Add("limit", count.ToString());
        query.Add("location", "footer");
        query.Add("expand", "history,body.view");

        var uriBuilder = new UriBuilder(
            $"{httpClient.BaseAddress}confluence/rest/api/content/{pageId}/child/comment"
        )
        {
            Query = query.ToString(),
        };

        var uri = uriBuilder.Uri;

        var responseMessage = await httpClient
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
