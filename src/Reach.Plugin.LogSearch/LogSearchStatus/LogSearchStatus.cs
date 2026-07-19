using System.CommandLine;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Reach.Plugin.Abstractions;
using Reach.Plugin.LogSearch.LogSearchStart;

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

        searchStatusCommand.SetAction(
            async (parseResult, cancellationToken) =>
            {
                var correlationId = parseResult.GetRequiredValue(correlationIdOption);

                var payload = new SearchStatusCliCommandPayload(correlationId);

                var result = await _handler.Handle(payload, cancellationToken);

                return result.StatusCode;
            }
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
            return new LogSearchStatusResult.Success(storedValue.Result);
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
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcPayload,
            LogSearchStatusPayload
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<RpcResult, LogSearchStatusResult>();

        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStatusResult.NotFound
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStatusResult.Ongoing
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStatusResult.Success
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStatusResult.Expired
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStatusResult.Error
        >();

        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStatusResult,
            LogSearchStatusResult.NotFound
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStatusResult,
            LogSearchStatusResult.Ongoing
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStatusResult,
            LogSearchStatusResult.Success
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStatusResult,
            LogSearchStatusResult.Expired
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStatusResult,
            LogSearchStatusResult.Error
        >();
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

internal abstract record LogSearchStatusResult : RpcResult
{
    public sealed record NotFound : LogSearchStatusResult;

    public sealed record Ongoing : LogSearchStatusResult;

    public sealed record Success(SearchFileCommandResult Value) : LogSearchStatusResult;

    public sealed record Expired : LogSearchStatusResult;

    public sealed record Error(string Message) : LogSearchStatusResult;
}
