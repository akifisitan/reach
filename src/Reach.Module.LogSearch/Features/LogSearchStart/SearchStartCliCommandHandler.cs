namespace Reach.Module.LogSearch.Features.LogSearchStart;

internal sealed class SearchStartCliCommandHandler
    : CliCommandHandler<SearchStartCliCommandPayload, SearchStartCliCommandResult>
{
    private readonly ILogger<SearchStartCliCommandHandler> _logger;
    private readonly IRpcClient _rpcClient;

    public SearchStartCliCommandHandler(
        ILogger<SearchStartCliCommandHandler> logger,
        IRpcClient rpcClient
    )
    {
        _logger = logger;
        _rpcClient = rpcClient;
    }

    protected override async Task<SearchStartCliCommandResult> Handle(
        SearchStartCliCommandPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var startPayload = new LogSearchStartPayload(
                payload.Query,
                payload.ServerName,
                payload.StartTime,
                payload.EndTime
            );

            var result = await _rpcClient.Invoke<LogSearchStartPayload, LogSearchStartResult>(
                startPayload,
                cancellationToken
            );

            switch (result)
            {
                case LogSearchStartResult.Success success:
                {
                    Console.WriteLine($"Can poll with {success.CorrelationId}");

                    return new(0);
                }
                case LogSearchStartResult.Error err:
                {
                    Console.WriteLine(err.Message);

                    return new(1);
                }
                default:
                    throw new NotSupportedException();
            }
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
