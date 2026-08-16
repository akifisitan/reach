using System.CommandLine;

namespace Reach.Module.Example.Echo.Features.Echo;

internal sealed class EchoCliCommand : IRootCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handlerDispatcher;
    public Command Command { get; }

    public EchoCliCommand(IHandlerDispatcher<CliCommandPayload, CliCommandResult> handlerDispatcher)
    {
        _handlerDispatcher = handlerDispatcher;
        Command = new Command("echo", "Echo a message");

        var messageOption = new Option<string>("--message", "-m")
        {
            Description = "Message to send",
            Required = true,
        };

        var pollMsOption = new Option<int>("--poll-ms", "-ms")
        {
            Description = "Poll interval in milliseconds",
            DefaultValueFactory = static _ => 5000,
        };

        pollMsOption.Validators.Add(static x =>
        {
            var value = x.GetValueOrDefault<int>();

            if (value is < 1000 or > 5000)
            {
                x.AddError("Poll interval must be between 1000 and 5000 milliseconds.");
            }
        });

        Command.Add(messageOption);
        Command.Add(pollMsOption);

        Command.SetPayloadAction(
            parseResult => new EchoCliCommandPayload(
                parseResult.GetRequiredValue(messageOption),
                parseResult.GetRequiredValue(pollMsOption)
            ),
            _handlerDispatcher
        );
    }
}
