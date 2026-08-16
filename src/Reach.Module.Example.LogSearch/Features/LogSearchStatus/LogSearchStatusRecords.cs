namespace Reach.Module.Example.LogSearch.Features.LogSearchStatus;

internal sealed record LogSearchStatusCliCommandPayload(string CorrelationId) : CliCommandPayload;

internal sealed record LogSearchStatusCliCommandResult(int StatusCode)
    : CliCommandResult(StatusCode);

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
