using Reach.Module.Toolkit.ResultStore;

namespace Reach.Module.Example.Echo.Features.Echo;

internal sealed class EchoRpcHandler : RemoteCommandRpcHandler<EchoPayload, EchoCommand>
{
    private readonly ILogger<EchoRpcHandler> _logger;
    private readonly IResultStore<EchoCommandResult> _resultStore;

    public EchoRpcHandler(
        ILogger<EchoRpcHandler> logger,
        IResultStore<EchoCommandResult> resultStore
    )
    {
        _logger = logger;
        _resultStore = resultStore;
    }

    protected override async Task<EchoCommand> PrepareCommand(
        EchoPayload payload,
        CancellationToken cancellationToken
    )
    {
        var id = Guid.CreateVersion7().ToString("N");

        var command = new EchoCommand(id, payload.Message);

        _logger.ZLogInformation($"[{id}] Preparing {command} for remote submission");

        await _resultStore.Insert(id, cancellationToken);

        return command;
    }

    protected override async Task HandleSubmissionFailure(
        EchoCommand command,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        await _resultStore.TrySetAsFailed(command.Id, exception.Message, cancellationToken);
    }
}
