using System.CommandLine;
using Reach.Module.LogSearch.Features.LogSearchStart;
using Reach.Module.LogSearch.Features.LogSearchStatus;

namespace Reach.Module.LogSearch;

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
