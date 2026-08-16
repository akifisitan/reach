using System.CommandLine;

namespace Reach.Module.LogSearch.Features.LogSearchStart;

internal sealed class SearchStartCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handlerDispatcher;

    public SearchStartCliCommand(
        IHandlerDispatcher<CliCommandPayload, CliCommandResult> handlerDispatcher
    )
    {
        _handlerDispatcher = handlerDispatcher;
    }

    public void Register(Command rootSearchCommand)
    {
        var searchStartCommand = new Command("start", "Search in log");

        rootSearchCommand.Subcommands.Add(searchStartCommand);

        var serverNameOption = new Option<string>("--server-name", "-s")
        {
            Description = "Server Name",
            Required = true,
        };

        var queryOption = new Option<string>("--query", "-q")
        {
            Description = "Query to search for",
            Required = true,
        };

        var startTimeOption = new Option<DateTimeOffset>("--start-time", "-start")
        {
            Description = "Start time",
            Required = false,
            DefaultValueFactory = x => DateTimeOffset.Now,
        };

        var endTimeOption = new Option<DateTimeOffset>("--end-time", "-end")
        {
            Description = "End time",
            Required = false,
            DefaultValueFactory = x => DateTimeOffset.Now,
        };

        searchStartCommand.Add(serverNameOption);
        searchStartCommand.Add(queryOption);
        searchStartCommand.Add(startTimeOption);
        searchStartCommand.Add(endTimeOption);

        searchStartCommand.SetPayloadAction(
            parseResult => new SearchStartCliCommandPayload(
                parseResult.GetRequiredValue(queryOption),
                parseResult.GetRequiredValue(serverNameOption),
                parseResult.GetRequiredValue(startTimeOption),
                parseResult.GetRequiredValue(endTimeOption)
            ),
            _handlerDispatcher
        );
    }
}
