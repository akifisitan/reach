using System.CommandLine;
using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed class PingCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handler;

    public PingCliCommand(IHandlerDispatcher<CliCommandPayload, CliCommandResult> handler)
    {
        _handler = handler;
    }

    public void Register(Command rootExampleCommand)
    {
        var pingCommand = new Command("ping", "Send a ping to the remote agent");

        rootExampleCommand.Subcommands.Add(pingCommand);

        var messageOption = new Option<string>("--message", "-m")
        {
            Description = "Message to send",
            Required = true,
        };

        pingCommand.Add(messageOption);

        pingCommand.SetPayloadAction(
            parseResult => new PingCliCommandPayload(parseResult.GetRequiredValue(messageOption)),
            _handler
        );
    }
}
