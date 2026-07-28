using System.CommandLine;

namespace Reach.Module.LogSearch.Features.LogSearchStatus;

internal sealed class SearchStatusCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handler;

    public SearchStatusCliCommand(IHandlerDispatcher<CliCommandPayload, CliCommandResult> handler)
    {
        _handler = handler;
    }

    public void Register(Command rootSearchCommand)
    {
        var searchStatusCommand = new Command("status", "Search status");

        rootSearchCommand.Subcommands.Add(searchStatusCommand);

        var correlationIdOption = new Option<string>("--correlation-id")
        {
            Description = "Correlation Id",
            Required = true,
        };

        searchStatusCommand.Add(correlationIdOption);

        searchStatusCommand.SetPayloadAction(
            parseResult => new SearchStatusCliCommandPayload(
                parseResult.GetRequiredValue(correlationIdOption)
            ),
            _handler
        );
    }
}
