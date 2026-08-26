using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Reach.Relay.LocalFile;

public sealed class LocalFileRelay : ILocalRelay, IRemoteRelay
{
    private const string LocalToRemoteCursorName = "localfile.local_to_remote";
    private const string RemoteToLocalCursorName = "localfile.remote_to_local";

    private readonly LocalFileConfigurationStore _store;
    private readonly IRelayPayloadSerializer _payloadSerializer;
    private readonly ILogger<LocalFileRelay> _logger;
    private readonly IRelayCursorStorage _cursorStorage;

    private LocalFileRelayOptions Options => _store.Current;

    public LocalFileRelay(
        LocalFileConfigurationStore configurationStore,
        IRelayPayloadSerializer payloadSerializer,
        ILogger<LocalFileRelay> logger,
        IRelayCursorStorage cursorStorage
    )
    {
        _store = configurationStore;
        _payloadSerializer = payloadSerializer;
        _logger = logger;
        _cursorStorage = cursorStorage;
    }

    public async Task<PostToRemoteResult> PostToRemote(
        RelayCommand value,
        CancellationToken cancellationToken
    )
    {
        var payload = _payloadSerializer.Serialize(value);

        await CreateLogEntry(payload, Options.LocalToRemoteFilePath, cancellationToken)
            .ConfigureAwait(false);

        return new(new(value.Id));
    }

    public async Task<PostToLocalResult> PostToLocal(
        RelayCommandResult value,
        CancellationToken cancellationToken
    )
    {
        var payload = _payloadSerializer.Serialize(value);

        await CreateLogEntry(payload, Options.RemoteToLocalFilePath, cancellationToken)
            .ConfigureAwait(false);

        return new();
    }

    public IAsyncEnumerable<RelayDelivery<RelayCommand>> PollFromLocal(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommand>(
            Options.LocalToRemoteFilePath,
            LocalToRemoteCursorName,
            options,
            cancellationToken
        );
    }

    public IAsyncEnumerable<RelayDelivery<RelayCommandResult>> PollFromRemote(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return InnerPoll<RelayCommandResult>(
            Options.RemoteToLocalFilePath,
            RemoteToLocalCursorName,
            options,
            cancellationToken
        );
    }

    private async IAsyncEnumerable<RelayDelivery<TOut>> InnerPoll<TOut>(
        string filePath,
        string cursorName,
        PollingOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var defaultCursor = new RelayCursor(
            0,
            DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1)),
            0
        );
        var cursor =
            await _cursorStorage.Retrieve(cursorName, cancellationToken).ConfigureAwait(false)
            ?? defaultCursor;

        HashSet<string> processedEntries = [];

        var pollDelayMs = Options.PollDelayMs;

        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Storage> response;

            try
            {
                response = await ReadLogEntries(filePath, cursor, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                _logger.ZLogError($"{ex.Message}. Resetting cursor");

                cursor = defaultCursor;

                await _cursorStorage
                    .Store(cursorName, cursor, cancellationToken)
                    .ConfigureAwait(false);

                continue;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"An error occurred");

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            if (response.Count == 0)
            {
                _logger.ZLogTrace($"No new lines found");

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                continue;
            }

            var baseIndex = cursor.CurrentIndex;

            for (var i = 0; i < response.Count; i++)
            {
                var resultContent = response[i];
                var commentId = resultContent.Id;

                if (processedEntries.Contains(commentId))
                {
                    _logger.ZLogDebug($"Skipping already processed entry: {commentId}");

                    continue;
                }

                if (cursor.CreationTime > resultContent.CreatedDate)
                {
                    _logger.ZLogDebug($"Skipping line created before cursor: {commentId}");

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

                    var skippedCursor = new RelayCursor(
                        baseIndex + i + 1,
                        resultContent.CreatedDate >= cursor.CreationTime
                            ? resultContent.CreatedDate
                            : cursor.CreationTime,
                        0
                    );

                    await _cursorStorage
                        .Store(cursorName, skippedCursor, cancellationToken)
                        .ConfigureAwait(false);

                    cursor = skippedCursor;

                    continue;
                }

                await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);

                processedEntries.Add(commentId);

                var nextCursor = new RelayCursor(
                    baseIndex + i + 1,
                    resultContent.CreatedDate >= cursor.CreationTime
                        ? resultContent.CreatedDate
                        : cursor.CreationTime,
                    0
                );

                yield return new(
                    command,
                    async ct =>
                    {
                        await _cursorStorage
                            .Store(cursorName, nextCursor, ct)
                            .ConfigureAwait(false);
                        cursor = nextCursor;
                    }
                );
            }

            await Task.Delay(pollDelayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<List<Storage>> ReadLogEntries(
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

    private static async Task CreateLogEntry(
        string text,
        string filePath,
        CancellationToken cancellationToken
    )
    {
        var base64Text = text.EncodeBase64();

        await File.AppendAllLinesAsync(
                filePath,
                [
                    JsonSerializer.Serialize(
                        new Storage(
                            Guid.CreateVersion7().ToString("N"),
                            base64Text,
                            DateTimeOffset.Now
                        ),
                        LocalFileJsonSerializerContext.Default.Storage
                    ),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
