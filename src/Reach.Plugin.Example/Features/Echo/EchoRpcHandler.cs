using Reach.Plugin.Toolkit.ResultStore;
using Reach.Relay;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoRpcHandler : RpcHandler<EchoPayload, EchoResult>
{
    private readonly ILogger<EchoRpcHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly IResultStore<EchoCommandResult> _resultStore;

    public EchoRpcHandler(
        ILogger<EchoRpcHandler> logger,
        ILocalRelay relay,
        IResultStore<EchoCommandResult> resultStore
    )
    {
        _logger = logger;
        _relay = relay;
        _resultStore = resultStore;
    }

    protected override async Task<EchoResult> Handle(
        EchoPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new EchoCommand(payload.CorrelationId, payload.Message);

            _logger.ZLogInformation($"[{command.Id}] Posting {command} to remote");

            await _resultStore.Insert(payload.CorrelationId, cancellationToken);

            var result = await _relay.PostToRemote(command, cancellationToken);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            return new EchoResult.Success(payload.CorrelationId);
        }
        catch (ResultAlreadyExistsException ex)
        {
            _logger.ZLogError(ex, $"[{payload.CorrelationId}] Echo already exists");

            return new EchoResult.Error(
                $"An echo with correlation id '{payload.CorrelationId}' already exists. Check its status."
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            await _resultStore.TrySetAsFailed(payload.CorrelationId, ex.Message, cancellationToken);

            return new EchoResult.Error(ex.Message);
        }
    }
}
