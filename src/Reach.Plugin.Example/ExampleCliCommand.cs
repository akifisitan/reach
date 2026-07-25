using System.CommandLine;
using Reach.Plugin.Abstractions;
using Reach.Plugin.Example.Features.Ping;

namespace Reach.Plugin.Example;

internal sealed class ExampleCliCommand : ICliCommand
{
    public Command Command { get; }

    public ExampleCliCommand(PingCliCommand pingCliCommand)
    {
        Command = new Command("example", "Example plugin");

        pingCliCommand.Register(Command);
    }
}
