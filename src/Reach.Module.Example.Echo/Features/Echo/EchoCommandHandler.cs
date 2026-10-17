namespace Reach.Module.Example.Echo.Features.Echo;

internal sealed class EchoCommandHandler : RelayCommandHandler<EchoCommand, EchoCommandResult>
{
    private readonly ILogger<EchoCommandHandler> _logger;
    private readonly IEchoFormatter _echoFormatter;

    public EchoCommandHandler(ILogger<EchoCommandHandler> logger, IEchoFormatter echoFormatter)
    {
        _logger = logger;
        _echoFormatter = echoFormatter;
    }

    protected override async Task<EchoCommandResult> Handle(
        EchoCommand command,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Running remote execute for {command}");

        await Task.Delay(1500, cancellationToken).ConfigureAwait(false);

        return new EchoCommandResult(command.Id, _echoFormatter.Format(command.Message));
    }
}
