using System.CommandLine;
using Reach.Plugin.Example.Features.Echo;

namespace Reach.Plugin.Example;

internal sealed class ExampleCliCommand : ICliCommand
{
    public Command Command { get; }

    public ExampleCliCommand(EchoCliCommand echoCliCommand)
    {
        Command = new Command("example", "Example plugin");

        echoCliCommand.Register(Command);
    }
}
