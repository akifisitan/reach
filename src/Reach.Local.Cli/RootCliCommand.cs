using System.CommandLine;
using Reach.Plugin.Abstractions;

namespace Reach.Local.Cli;

internal sealed record RootCliCommand
{
    private readonly RootCommand _root;

    public RootCliCommand(IEnumerable<ICliCommand> commands)
    {
        var rootCommand = new RootCommand();

        foreach (var command in commands)
        {
            rootCommand.Subcommands.Add(command.Command);
        }

        _root = rootCommand;
    }

    public Task Invoke(string[] args)
    {
        return _root.Parse(args).InvokeAsync();
    }
}
