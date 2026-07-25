using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingCommandHandler : RelayCommandHandler<PingCommand, PingCommandResult>
{
    private readonly ILogger<PingCommandHandler> _logger;

    public PingCommandHandler(ILogger<PingCommandHandler> logger)
    {
        _logger = logger;
    }

    protected override Task<PingCommandResult> Handle(
        PingCommand command,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Running remote execute for {command}");

        return Task.FromResult(new PingCommandResult($"pong: {command.Message}", command));
    }
}
