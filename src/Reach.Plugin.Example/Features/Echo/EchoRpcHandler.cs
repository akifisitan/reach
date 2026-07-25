using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoRpcHandler : RpcHandler<EchoPayload, EchoResult>
{
    private readonly ILogger<EchoRpcHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly EchoResultStore _echoResultStore;

    public EchoRpcHandler(
        ILogger<EchoRpcHandler> logger,
        ILocalRelay relay,
        EchoResultStore echoResultStore
    )
    {
        _logger = logger;
        _relay = relay;
        _echoResultStore = echoResultStore;
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

            _echoResultStore.Insert(payload.CorrelationId);

            var result = await _relay.PostToRemote(command, cancellationToken);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            return new EchoResult.Success(payload.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            return new EchoResult.Error(ex.Message);
        }
    }
}
