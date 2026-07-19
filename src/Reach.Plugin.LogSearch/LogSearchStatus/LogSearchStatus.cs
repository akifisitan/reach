using System.CommandLine;
using Reach.Plugin.Abstractions;

namespace Reach.Plugin.LogSearch.LogSearchStatus;

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

internal sealed class LogSearchStatusHandler
    : RpcHandler<LogSearchStatusPayload, LogSearchStatusResult>
{
    private readonly ILogger<LogSearchStatusHandler> _logger;
    private readonly SearchResultStore _searchResultStore;

    public LogSearchStatusHandler(
        ILogger<LogSearchStatusHandler> logger,
        SearchResultStore searchResultStore
    )
    {
        _logger = logger;
        _searchResultStore = searchResultStore;
    }

    protected override async Task<LogSearchStatusResult> Handle(
        LogSearchStatusPayload payload,
        CancellationToken cancellationToken
    )
    {
        var storedValue = _searchResultStore.GetValue(payload.CorrelationId);

        if (storedValue is null)
        {
            return new LogSearchStatusResult.NotFound();
        }

        if (storedValue.Result is not null)
        {
            return new LogSearchStatusResult.Success(new(storedValue.Result.Content));
        }

        //if (storedValue.Result is SomeErrorType)
        //{
        //    return new LogSearchStatusResult.Error("error");
        //}

        return new LogSearchStatusResult.Ongoing();
    }
}

internal sealed partial class LogSearchStatusJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => LogSearchStatusJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedType<RpcPayload, LogSearchStatusPayload>("logSearch.status");
        jsonTypeInfo.RegisterResultFamily<RpcResult, LogSearchStatusResult>(
            "logSearch.status.result",
            (typeof(LogSearchStatusResult.NotFound), "logSearch.status.result.notFound"),
            (typeof(LogSearchStatusResult.Ongoing), "logSearch.status.result.ongoing"),
            (typeof(LogSearchStatusResult.Success), "logSearch.status.result.success"),
            (typeof(LogSearchStatusResult.Expired), "logSearch.status.result.expired"),
            (typeof(LogSearchStatusResult.Error), "logSearch.status.result.error")
        );
    }

    [JsonSerializable(typeof(LogSearchStatusPayload))]
    [JsonSerializable(typeof(LogSearchStatusResult))]
    [JsonSerializable(typeof(LogSearchStatusResult.NotFound))]
    [JsonSerializable(typeof(LogSearchStatusResult.Ongoing))]
    [JsonSerializable(typeof(LogSearchStatusResult.Success))]
    [JsonSerializable(typeof(LogSearchStatusResult.Expired))]
    [JsonSerializable(typeof(LogSearchStatusResult.Error))]
    private sealed partial class LogSearchStatusJsonSerializerContext : JsonSerializerContext;
}

internal sealed record SearchStatusCliCommandPayload(string CorrelationId) : CliCommandPayload;

internal sealed record SearchStatusCliCommandResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record LogSearchStatusPayload(string CorrelationId) : RpcPayload;

internal sealed record LogSearchStatusSuccessResult(string Content);

internal abstract record LogSearchStatusResult : RpcResult
{
    public sealed record NotFound : LogSearchStatusResult;

    public sealed record Ongoing : LogSearchStatusResult;

    public sealed record Success(LogSearchStatusSuccessResult Value) : LogSearchStatusResult;

    public sealed record Expired : LogSearchStatusResult;

    public sealed record Error(string Message) : LogSearchStatusResult;
}
