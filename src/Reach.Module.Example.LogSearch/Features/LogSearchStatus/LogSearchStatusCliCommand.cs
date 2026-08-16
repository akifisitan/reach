using System.CommandLine;

namespace Reach.Module.Example.LogSearch.Features.LogSearchStatus;

internal sealed class LogSearchStatusCliCommand : IChildCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handlerDispatcher;

    public LogSearchStatusCliCommand(
        IHandlerDispatcher<CliCommandPayload, CliCommandResult> handlerDispatcher
    )
    {
        _handlerDispatcher = handlerDispatcher;
    }

    public void AddTo(Command rootSearchCommand)
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
            parseResult => new LogSearchStatusCliCommandPayload(
                parseResult.GetRequiredValue(correlationIdOption)
            ),
            _handlerDispatcher
        );
    }
}
