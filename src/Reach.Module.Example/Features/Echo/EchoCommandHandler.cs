namespace Reach.Module.Example.Features.Echo;

internal sealed class EchoCommandHandler : RelayCommandHandler<EchoCommand, EchoCommandResult>
{
    private readonly ILogger<EchoCommandHandler> _logger;

    public EchoCommandHandler(ILogger<EchoCommandHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<EchoCommandResult> Handle(
        EchoCommand command,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Running remote execute for {command}");

        await Task.Delay(1500, cancellationToken);

        return new EchoCommandResult(command.Id, $"echo: {command.Message}");
    }
}
