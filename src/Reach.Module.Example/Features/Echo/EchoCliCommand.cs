using System.CommandLine;

namespace Reach.Module.Example.Features.Echo;

internal sealed class EchoCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handler;

    public EchoCliCommand(IHandlerDispatcher<CliCommandPayload, CliCommandResult> handler)
    {
        _handler = handler;
    }

    public void Register(Command rootExampleCommand)
    {
        var echoCommand = new Command("echo", "Send an echo to the remote agent");

        rootExampleCommand.Subcommands.Add(echoCommand);

        var messageOption = new Option<string>("--message", "-m")
        {
            Description = "Message to send",
            Required = true,
        };

        var pollMsOption = new Option<int>("--poll-ms", "-ms")
        {
            Description = "Poll interval in milliseconds",
            DefaultValueFactory = static _ => 2000,
        };

        pollMsOption.Validators.Add(static x =>
        {
            var value = x.GetValueOrDefault<int>();

            if (value is < 1000 or > 5000)
            {
                x.AddError("Poll interval must be between 1000 and 5000 milliseconds.");
            }
        });

        echoCommand.Add(messageOption);
        echoCommand.Add(pollMsOption);

        echoCommand.SetPayloadAction(
            parseResult => new EchoCliCommandPayload(
                parseResult.GetRequiredValue(messageOption),
                parseResult.GetRequiredValue(pollMsOption)
            ),
            _handler
        );
    }
}
