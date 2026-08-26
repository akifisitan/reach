using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.Example.LogSearch.Features.LogSearchStart;

internal sealed class LogSearchStartHandler
    : RemoteCommandRpcHandler<LogSearchStartPayload, SearchFileCommand>
{
    private readonly ILogger<LogSearchStartHandler> _logger;
    private readonly IResultStore<SearchFileCommandResult> _resultStore;

    public LogSearchStartHandler(
        ILogger<LogSearchStartHandler> logger,
        IResultStore<SearchFileCommandResult> resultStore
    )
    {
        _logger = logger;
        _resultStore = resultStore;
    }

    protected override async Task<SearchFileCommand> PrepareCommand(
        LogSearchStartPayload payload,
        CancellationToken cancellationToken
    )
    {
        var id = Guid.CreateVersion7().ToString("N");

        var command = new SearchFileCommand(
            id,
            payload.Query,
            payload.ServerName,
            payload.StartTime,
            payload.EndTime
        );

        _logger.ZLogInformation($"[{command.Id}] Preparing {command} for remote submission");

        await _resultStore.Insert(id, cancellationToken).ConfigureAwait(false);

        return command;
    }

    protected override async Task HandleSubmissionFailure(
        SearchFileCommand command,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        await _resultStore
            .TrySetAsFailed(command.Id, exception.Message, cancellationToken)
            .ConfigureAwait(false);
    }
}
