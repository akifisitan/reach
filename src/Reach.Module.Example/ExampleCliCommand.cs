using System.CommandLine;
using Reach.Module.Example.Features.Echo;

namespace Reach.Module.Example;

internal sealed class ExampleCliCommand : ICliCommand
{
    public Command Command { get; }

    public ExampleCliCommand(EchoCliCommand echoCliCommand)
    {
        Command = new Command("example", "Example module");

        echoCliCommand.Register(Command);
    }
}
