using Reach.Module.Toolkit.ResultStore;
using Reach.Relay;

namespace Reach.Module.Example.Features.Echo;

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
        var id = Guid.CreateVersion7().ToString("N");

        try
        {
            var command = new EchoCommand(id, payload.Message);

            _logger.ZLogInformation($"[{id}] Posting {command} to remote");

            await _resultStore.Insert(id, cancellationToken);

            var result = await _relay.PostToRemote(command, cancellationToken);

            _logger.ZLogInformation($"[{id}] Posting successful {result.PollId.Value}");

            return new EchoResult.Success(result.PollId.Value);
        }
        catch (ResultAlreadyExistsException ex)
        {
            _logger.ZLogError(ex, $"[{id}] Echo already exists");

            return new EchoResult.Error(
                $"An echo with correlation id '{id}' already exists. Check its status."
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            await _resultStore.TrySetAsFailed(id, ex.Message, cancellationToken);

            return new EchoResult.Error(ex.Message);
        }
    }
}
