using Reach.Cli;

namespace Reach.Module.SoapSearch.Features.SoapSearchStatus;

internal sealed class SoapSearchStatusCliCommandHandler
    : CliCommandHandler<SoapSearchStatusCliPayload, SoapSearchStatusCliResult>
{
    private readonly IRpcClient _client;
    private readonly IConsoleWriter _console;

    public SoapSearchStatusCliCommandHandler(IRpcClient client, IConsoleWriter console)
    {
        _client = client;
        _console = console;
    }

    protected override async Task<SoapSearchStatusCliResult> Handle(
        SoapSearchStatusCliPayload payload,
        CancellationToken cancellationToken
    )
    {
        var result = await _client
            .Invoke<SoapSearchStatusPayload, SoapSearchStatusResult>(
                new(payload.CorrelationId),
                cancellationToken
            )
            .ConfigureAwait(false);
        if (result is SoapSearchStatusResult.Success success)
        {
            _console.WriteLine(success.Value.Outcome.ToString());
            _console.WriteLine(success.Value.Request ?? "Request: null");
            _console.WriteLine(success.Value.Response ?? "Response: null");
            foreach (var error in success.Value.Errors)
            {
                _console.WriteLine($"{error.Source}:{error.LineNumber}: {error.Message}");
            }

            return new(success.Value.Outcome == SoapSearchOutcome.Complete ? 0 : 1);
        }

        _console.WriteLine(result.ToString());
        return new(
            result
                is SoapSearchStatusResult.Error
                    or SoapSearchStatusResult.Expired
                    or SoapSearchStatusResult.NotFound
                ? 1
                : 0
        );
    }
}
