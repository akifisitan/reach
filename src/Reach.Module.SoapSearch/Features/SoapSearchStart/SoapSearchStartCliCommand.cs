using System.CommandLine;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed class SoapSearchStartCliCommand : IChildCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _dispatcher;

    public SoapSearchStartCliCommand(
        IHandlerDispatcher<CliCommandPayload, CliCommandResult> dispatcher
    )
    {
        _dispatcher = dispatcher;
    }

    public void AddTo(Command command)
    {
        var start = new Command("start", "Start a SOAP envelope search");
        var siid = new Option<string>("--siid") { Required = true };
        var application = new Option<string>("--application") { Required = true };
        var servers = new Option<string[]>("--servers")
        {
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };
        var startTime = new Option<DateTimeOffset>("--start-time") { Required = true };
        var endTime = new Option<DateTimeOffset>("--end-time") { Required = true };
        var strategy = new Option<SoapSearchStrategy>("--strategy")
        {
            Required = true,
            Description = "Live, Archive or Both",
        };
        start.Add(siid);
        start.Add(application);
        start.Add(servers);
        start.Add(startTime);
        start.Add(endTime);
        start.Add(strategy);
        start.SetPayloadAction(
            parse => new SoapSearchStartCliPayload(
                new(
                    parse.GetRequiredValue(siid),
                    parse.GetRequiredValue(application),
                    parse.GetRequiredValue(servers),
                    parse.GetRequiredValue(startTime),
                    parse.GetRequiredValue(endTime),
                    parse.GetRequiredValue(strategy)
                )
            ),
            _dispatcher
        );
        command.Subcommands.Add(start);
    }
}
