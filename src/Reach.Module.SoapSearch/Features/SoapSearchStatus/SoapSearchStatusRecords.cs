namespace Reach.Module.SoapSearch.Features.SoapSearchStatus;

internal sealed record SoapSearchStatusCliPayload(string CorrelationId) : CliCommandPayload;

internal sealed record SoapSearchStatusCliResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record SoapSearchStatusPayload(string CorrelationId) : RpcPayload;

internal abstract record SoapSearchStatusResult : RpcResult
{
    public sealed record NotFound : SoapSearchStatusResult;

    public sealed record Ongoing : SoapSearchStatusResult;

    public sealed record Success(SoapSearchResult Value) : SoapSearchStatusResult;

    public sealed record Expired : SoapSearchStatusResult;

    public sealed record Error(string Message) : SoapSearchStatusResult;
}
