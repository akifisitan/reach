using System.CommandLine;
using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch.LogSearchStart;
using Reach.Plugin.LogSearch.LogSearchStatus;

namespace Reach.Plugin.LogSearch;

internal sealed class SearchCliCommand : ICliCommand
{
    public Command Command { get; }

    public SearchCliCommand(
        SearchStartCliCommand searchStartCliCommand,
        SearchStatusCliCommand searchStatusCliCommand
    )
    {
        Command = new Command("search", "Search operations");

        searchStartCliCommand.Register(Command);
        searchStatusCliCommand.Register(Command);
    }
}
