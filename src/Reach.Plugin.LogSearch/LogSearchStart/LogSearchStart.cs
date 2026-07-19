using System.CommandLine;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.LogSearch.LogSearchStart;

internal sealed class SearchStartCliCommand
{
    private readonly IHandlerDispatcher<CliCommandPayload, CliCommandResult> _handler;

    public SearchStartCliCommand(IHandlerDispatcher<CliCommandPayload, CliCommandResult> handler)
    {
        _handler = handler;
    }

    public void Register(Command rootSearchCommand)
    {
        var searchStartCommand = new Command("start", "Search in log");

        rootSearchCommand.Subcommands.Add(searchStartCommand);

        var serverNameOption = new Option<string>("--server-name", "-s")
        {
            Description = "Server Name",
            Required = true,
        };

        var queryOption = new Option<string>("--query", "-q")
        {
            Description = "Query to search for",
            Required = true,
        };

        var startTimeOption = new Option<DateTimeOffset>("--start-time", "-start")
        {
            Description = "Start time",
            Required = false,
            DefaultValueFactory = x => DateTimeOffset.Now,
        };

        var endTimeOption = new Option<DateTimeOffset>("--end-time", "-end")
        {
            Description = "End time",
            Required = false,

            DefaultValueFactory = x => DateTimeOffset.Now,
        };

        searchStartCommand.Add(serverNameOption);
        searchStartCommand.Add(queryOption);
        searchStartCommand.Add(startTimeOption);
        searchStartCommand.Add(endTimeOption);

        searchStartCommand.SetAction(
            async (parseResult, cancellationToken) =>
            {
                var query = parseResult.GetRequiredValue(queryOption);
                var serverName = parseResult.GetRequiredValue(serverNameOption);
                var startTime = parseResult.GetRequiredValue(startTimeOption);
                var endTime = parseResult.GetRequiredValue(endTimeOption);

                var payload = new SearchStartCliCommandPayload(
                    query,
                    serverName,
                    startTime,
                    endTime
                );

                var result = await _handler.Handle(payload, cancellationToken);

                return result.StatusCode;
            }
        );
    }
}

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
                Guid.NewGuid().ToString("N"),
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

internal sealed class LogSearchStartHandler
    : RpcHandler<LogSearchStartPayload, LogSearchStartResult>
{
    private readonly ILogger<LogSearchStartHandler> _logger;
    private readonly ILocalRelay _relay;
    private readonly SearchResultStore _searchResultStore;

    public LogSearchStartHandler(
        ILogger<LogSearchStartHandler> logger,
        ILocalRelay relay,
        SearchResultStore searchResultStore
    )
    {
        _logger = logger;
        _relay = relay;
        _searchResultStore = searchResultStore;
    }

    protected override async Task<LogSearchStartResult> Handle(
        LogSearchStartPayload payload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new SearchFileCommand(
                payload.CorrelationId,
                payload.Query,
                payload.ServerName,
                payload.StartTime,
                payload.EndTime
            );

            _logger.ZLogInformation($"[{command.Id}] Posting {command} to remote");

            _searchResultStore.Insert(payload.CorrelationId); // TODO: insert, if it fails because of duplicate, error saying to check the status as it already exists

            var result = await _relay.PostToRemote(command, cancellationToken);

            _searchResultStore.Insert(payload.CorrelationId);

            _logger.ZLogInformation($"[{command.Id}] Posting successful {result.PollId.Value}");

            if (Random.Shared.Next(0, 2) == 1)
            {
                throw new Exception("Threw an error just for testing");
            }

            return new LogSearchStartResult.Success(payload.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"An error occurred");

            return new LogSearchStartResult.Error(ex.Message);
        }
    }
}

internal sealed class SearchFileCommandHandler
    : RelayCommandHandler<SearchFileCommand, SearchFileCommandResult>
{
    private readonly FileSearcher _fileSearcher;
    private readonly ILogger<SearchFileCommandHandler> _logger;

    public SearchFileCommandHandler(
        FileSearcher fileSearcher,
        ILogger<SearchFileCommandHandler> logger
    )
    {
        _fileSearcher = fileSearcher;
        _logger = logger;
    }

    protected override async Task<SearchFileCommandResult> Handle(
        SearchFileCommand command,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Running server execute for {command}");

        var result = await _fileSearcher.Search(command.Query, cancellationToken);

        return new SearchFileCommandResult(result.Content, command);
    }
}

internal sealed class SearchFileCommandResultHandler
    : RelayCommandResultHandler<SearchFileCommandResult>
{
    private readonly ILogger<SearchFileCommandResultHandler> _logger;
    private readonly SearchResultStore _searchResultStore;

    public SearchFileCommandResultHandler(
        ILogger<SearchFileCommandResultHandler> logger,
        SearchResultStore searchResultStore
    )
    {
        _logger = logger;
        _searchResultStore = searchResultStore;
    }

    protected override async Task<Unit> Handle(
        SearchFileCommandResult result,
        CancellationToken cancellationToken
    )
    {
        _logger.ZLogInformation($"Doing something with command: {result.Id}");

        if (Random.Shared.Next(0, 2) == 1)
        {
            throw new Exception($"Threw an error for testing");
        }

        if (!_searchResultStore.TrySetAsComplete(result))
        {
            _logger.ZLogError($"What happened to the existing value?");

            return new();
        }

        _logger.ZLogInformation($"Received value from remote successfully. Result: {result}");

        return new();
    }
}

internal sealed partial class LogSearchStartJsonRegistration : IJsonTypeInfoRegistration
{
    public IJsonTypeInfoResolver Resolver => LogSearchStartJsonSerializerContext.Default;

    public void Configure(JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcPayload,
            LogSearchStartPayload
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<RpcResult, LogSearchStartResult>();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStartResult.Success
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RpcResult,
            LogSearchStartResult.Error
        >();

        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStartResult,
            LogSearchStartResult.Success
        >();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            LogSearchStartResult,
            LogSearchStartResult.Error
        >();

        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<RelayCommand, SearchFileCommand>();
        jsonTypeInfo.RegisterDerivedWithTypeNameAsDiscriminator<
            RelayCommandResult,
            SearchFileCommandResult
        >();
    }

    [JsonSerializable(typeof(LogSearchStartPayload))]
    [JsonSerializable(typeof(LogSearchStartResult))]
    [JsonSerializable(typeof(LogSearchStartResult.Success))]
    [JsonSerializable(typeof(LogSearchStartResult.Error))]
    [JsonSerializable(typeof(SearchFileCommand))]
    [JsonSerializable(typeof(SearchFileCommandResult))]
    private sealed partial class LogSearchStartJsonSerializerContext : JsonSerializerContext;
}

internal sealed record SearchStartCliCommandPayload(
    string Query,
    string ServerName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
) : CliCommandPayload;

internal sealed record SearchStartCliCommandResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record LogSearchStartPayload(
    string CorrelationId,
    string Query,
    string ServerName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
) : RpcPayload;

internal abstract record LogSearchStartResult : RpcResult
{
    public sealed record Success(string CorrelationId) : LogSearchStartResult;

    public sealed record Error(string Message) : LogSearchStartResult;
}

internal sealed record SearchFileCommand(
    string Id,
    string Query,
    string ServerName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
) : RelayCommand(nameof(SearchFileCommand), Id);

internal sealed record SearchFileCommandResult(string Content, RelayCommand Command)
    : RelayCommandResult(Guid.NewGuid().ToString("N"), Command);
