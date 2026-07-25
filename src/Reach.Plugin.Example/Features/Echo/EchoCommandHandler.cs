using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed class EchoCommandHandler : RelayCommandHandler<EchoCommand, EchoCommandResult>
{
    private readonly ILogger<EchoCommandHandler> _logger;

    public EchoCommandHandler(ILogger<EchoCommandHandler> logger)
    {
        _logger = logger;
    }

    protected override Task<EchoCommandResult> Handle(
        EchoCommand command,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Running remote execute for {command}");

        return Task.FromResult(new EchoCommandResult($"echo: {command.Message}", command.Id));
    }
}
