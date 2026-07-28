namespace Reach.Module.LogSearch.Features.LogSearchStatus;

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
