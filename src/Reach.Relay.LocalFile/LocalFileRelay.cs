using System.Runtime.CompilerServices;
using System.Text.Json;
using Reach.Relay.Abstractions;

namespace Reach.Relay.LocalFile;

public sealed class LocalFileRelay : ILocalRelay, IRemoteRelay
{
    private readonly LocalFileRelayOptions _options;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly ILogger<LocalFileRelay> _logger;
    private readonly IRelayCursorStorage _cursorStorage;

    public LocalFileRelay(
        IOptions<LocalFileRelayOptions> options,
        IRelayPayloadSerializer payloadSerializer,
        ILogger<LocalFileRelay> logger,
        IRelayCursorStorage cursorStorage
    )
    {
        _options = options.Value;
        _payloadSerializer = payloadSerializer;
        _logger = logger;
        _cursorStorage = cursorStorage;
    }

    public IAsyncEnumerable<RelayCommand> PollFromLocal(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommand>(_options.LocalToRemoteFilePath, options, cancellationToken);
    }

    public IAsyncEnumerable<RelayCommandResult> PollFromRemote(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommandResult>(
            _options.RemoteToLocalFilePath,
            options,
            cancellationToken
        );
    }

    private async IAsyncEnumerable<TOut> InnerPoll<TOut>(
        string filePath,
        PollingOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var cursor =
            await _cursorStorage.Retrieve(cancellationToken)
            ?? new RelayCursor(0, DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)));

        HashSet<string> processedEntries = [];

        var pollDelayMs = 1000;

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Storage> response;

            try
            {
                response = await ReadCommentsFromPage(filePath, cursor, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred");

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            if (response.Count == 0)
            {
                _logger.ZLogInformation($"No new lines found");

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            foreach (var resultContent in response)
            {
                var commentId = resultContent.Id;

                if (processedEntries.Contains(commentId))
                {
                    _logger.ZLogInformation($"Skipping already processed entry: {commentId}");

                    continue;
                }

                if (cursor.CreationTime > resultContent.CreatedDate)
                {
                    _logger.ZLogInformation($"Skipping line created before cursor: {commentId}");

                    continue;
                }

                TOut command;

                try
                {
                    var base64Text = resultContent.Payload;

                    var jsonText = base64Text.DecodeBase64();

                    command = _payloadSerializer.Deserialize<TOut>(jsonText);
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"An error occurred");
                    continue;
                }

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                processedEntries.Add(commentId);

                yield return command;
            }

            var latestCreatedAt =
                response.LastOrDefault(x => x.CreatedDate >= cursor.CreationTime)?.CreatedDate
                ?? cursor.CreationTime;

            cursor = cursor with
            {
                CurrentIndex = cursor.CurrentIndex + response.Count,
                CreationTime = latestCreatedAt,
            };

            await _cursorStorage.Store(cursor, cancellationToken);

            await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<List<Storage>> ReadCommentsFromPage(
        string filePath,
        RelayCursor cursor,
        CancellationToken cancellationToken
    )
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken).ConfigureAwait(false);

        if (cursor.CurrentIndex > lines.Length)
        {
            throw new InvalidOperationException(
                $"Current index: {cursor.CurrentIndex} is larger than file length: {lines.Length}"
            );
        }

        List<Storage> result = [];

        for (var i = cursor.CurrentIndex; i < lines.Length; i++)
        {
            var line = lines[i];

            var value = JsonSerializer.Deserialize(
                line,
                LocalFileJsonSerializerContext.Default.Storage
            );

            ArgumentNullException.ThrowIfNull(value);

            result.Add(value);
        }

        return result;
    }

    public async Task<PostToRemoteResult> PostToRemote(
        RelayCommand value,
        CancellationToken cancellationToken
    )
    {
        var payload = _payloadSerializer.Serialize(value);

        var base64Text = payload.EncodeBase64();

        await CreateComment(base64Text, _options.LocalToRemoteFilePath, cancellationToken)
            .ConfigureAwait(false);

        return new(new(value.Id));
    }

    public async Task<PostToLocalResult> PostToLocal(
        RelayCommandResult value,
        CancellationToken cancellationToken
    )
    {
        var payload = _payloadSerializer.Serialize(value);

        var base64Text = payload.EncodeBase64();

        await CreateComment(base64Text, _options.RemoteToLocalFilePath, cancellationToken)
            .ConfigureAwait(false);

        return new();
    }

    private async Task CreateComment(
        string base64Text,
        string filePath,
        CancellationToken cancellationToken
    )
    {
        await File.AppendAllLinesAsync(
                filePath,
                [
                    JsonSerializer.Serialize(
                        new Storage(Guid.NewGuid().ToString("N"), base64Text, DateTimeOffset.Now),
                        LocalFileJsonSerializerContext.Default.Storage
                    ),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
