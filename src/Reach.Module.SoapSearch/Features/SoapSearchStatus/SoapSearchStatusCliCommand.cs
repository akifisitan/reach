using System.CommandLine;

namespace Reach.Module.SoapSearch.Features.SoapSearchStatus;

internal sealed class SoapSearchStatusCliCommand : IChildCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _dispatcher;

    public SoapSearchStatusCliCommand(
        IHandlerDispatcher<CliCommandPayload, CliCommandResult> dispatcher
    )
    {
        _dispatcher = dispatcher;
    }

    public void AddTo(Command command)
    {
        var status = new Command("status", "Get SOAP search results");
        var id = new Option<string>("--correlation-id") { Required = true };
        status.Add(id);
        status.SetPayloadAction(
            parse => new SoapSearchStatusCliPayload(parse.GetRequiredValue(id)),
            _dispatcher
        );
        command.Subcommands.Add(status);
    }
}
