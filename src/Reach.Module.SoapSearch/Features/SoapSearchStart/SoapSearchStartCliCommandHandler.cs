using Reach.Cli;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed class SoapSearchStartCliCommandHandler
    : CliCommandHandler<SoapSearchStartCliPayload, SoapSearchStartCliResult>
{
    private readonly IRpcClient _client;
    private readonly IConsoleWriter _console;

    public SoapSearchStartCliCommandHandler(IRpcClient client, IConsoleWriter console)
    {
        _client = client;
        _console = console;
    }

    protected override async Task<SoapSearchStartCliResult> Handle(
        SoapSearchStartCliPayload payload,
        CancellationToken cancellationToken
    )
    {
        payload.Request.Validate();
        var result = await _client
            .Invoke<SoapSearchStartPayload, RemoteCommandSubmissionResult>(
                new(payload.Request),
                cancellationToken
            )
            .ConfigureAwait(false);
        if (result is RemoteCommandSubmissionResult.Accepted accepted)
        {
            _console.WriteLine(
                $"Poll with soap-search status --correlation-id {accepted.CorrelationId}"
            );
            return new(0);
        }

        if (result is RemoteCommandSubmissionResult.Failed failed)
        {
            _console.WriteLine(failed.Error.Message);
        }

        return new(1);
    }
}
