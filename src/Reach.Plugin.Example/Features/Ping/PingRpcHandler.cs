using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingRpcHandler : RpcHandler<PingPayload, PingResult>
{
    private readonly ILogger<PingRpcHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly PingResultStore _pingResultStore;

    public PingRpcHandler(
        ILogger<PingRpcHandler> logger,
        ILocalRelay relay,
        PingResultStore pingResultStore
    )
    {
        _logger = logger;
        _relay = relay;
        _pingResultStore = pingResultStore;
    }

    protected override async Task<PingResult> Handle(
        PingPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new PingCommand(payload.CorrelationId, payload.Message);

            _logger.ZLogInformation($"[{command.Id}] Posting {command} to remote");

            _pingResultStore.Insert(payload.CorrelationId);

            var result = await _relay.PostToRemote(command, cancellationToken);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            return new PingResult.Success(payload.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            return new PingResult.Error(ex.Message);
        }
    }
}
