using System.CommandLine;
using Reach.Module.Example.LogSearch.Features.LogSearchStart;
using Reach.Module.Example.LogSearch.Features.LogSearchStatus;

namespace Reach.Module.Example.LogSearch;

internal sealed class LogSearchCliCommand : IRootCliCommand
{
    public Command Command { get; }

    public LogSearchCliCommand(
        LogSearchStartCliCommand searchStartCliCommand,
        LogSearchStatusCliCommand searchStatusCliCommand
    )
    {
        Command = new Command("search", "Search files and check search job status");

        searchStartCliCommand.AddTo(Command);
        searchStatusCliCommand.AddTo(Command);
    }
}
