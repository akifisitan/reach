using System.CommandLine;
using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

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

        echoCommand.Add(messageOption);

        echoCommand.SetPayloadAction(
            parseResult => new EchoCliCommandPayload(parseResult.GetRequiredValue(messageOption)),
            _handler
        );
    }
}
