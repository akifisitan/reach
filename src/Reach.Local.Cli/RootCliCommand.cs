using System.CommandLine;
using Reach.Module;

namespace Reach.Local.Cli;

internal sealed record RootCliCommand
{
    private readonly RootCommand _root;

    public RootCliCommand(IEnumerable<IRootCliCommand> commands)
    {
        var rootCommand = new RootCommand();

        foreach (var command in commands)
        {
            if (rootCommand.Subcommands.Any(x => x.Name == command.Command.Name))
            {
                throw new InvalidOperationException(
                    $"Duplicate CLI command '{command.Command.Name}'."
                );
            }

            rootCommand.Subcommands.Add(command.Command);
        }

        _root = rootCommand;
    }

    public Task Invoke(string[] args)
    {
        return _root.Parse(args).InvokeAsync();
    }
}
