using Reach.Relay;

namespace Reach.Module.Example.LogSearch.Features.LogSearchStart;

internal sealed record LogSearchStartCliCommandPayload(
    string Query,
    string ServerName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
) : CliCommandPayload;

internal sealed record LogSearchStartCliCommandResult(int StatusCode)
    : CliCommandResult(StatusCode);

internal sealed record LogSearchStartPayload(
    string Query,
    string ServerName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
) : RpcPayload;

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

internal sealed record SearchFileCommandResult(string CommandId, string Content, int LineNumber)
    : RelayCommandResult(CommandId),
        IWireRecord
{
    public static string Kind => "searchFile.result";
}
