using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.LogSearch.Features.LogSearchStart;

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
) : RelayCommand(Id), IWireRecord
{
    public static string Kind => "searchFile";
}

internal sealed record SearchFileCommandResult(string Content, RelayCommand Command)
    : RelayCommandResult(Guid.NewGuid().ToString("N"), Command),
        IWireRecord
{
    public static string Kind => "searchFile.result";
}
