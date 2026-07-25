using Reach.Plugin.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStatus;

internal sealed class SearchStatusCliCommandHandler
    : CliCommandHandler<SearchStatusCliCommandPayload, SearchStatusCliCommandResult>
{
    private readonly ILogger<SearchStatusCliCommandHandler> _logger;
    private readonly IRpcClient _localClient;

    public SearchStatusCliCommandHandler(
        ILogger<SearchStatusCliCommandHandler> logger,
        IRpcClient localClient
    )
    {
        _logger = logger;
        _localClient = localClient;
    }

    protected override async Task<SearchStatusCliCommandResult> Handle(
        SearchStatusCliCommandPayload cliPayload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var payload = new LogSearchStatusPayload(cliPayload.CorrelationId);

            var result = await _localClient.Invoke<LogSearchStatusPayload, LogSearchStatusResult>(
                payload,
                cancellationToken
            );

            Console.WriteLine($"Result: {result}");

            return new(0);
        }
        catch (OperationCanceledException)
        {
            return new(1);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            return new(1);
        }
    }
}
