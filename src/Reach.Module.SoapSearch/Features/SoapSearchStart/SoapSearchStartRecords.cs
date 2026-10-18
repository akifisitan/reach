using Reach.Relay;

namespace Reach.Module.SoapSearch.Features.SoapSearchStart;

internal sealed record SoapSearchStartCliPayload(SoapSearchRequest Request) : CliCommandPayload;

internal sealed record SoapSearchStartCliResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record SoapSearchStartPayload(SoapSearchRequest Request) : RpcPayload;

internal sealed record SoapSearchCommand(string Id, SoapSearchRequest Request)
    : RelayCommand(Id),
        IWireRecord
{
    public static string Kind => "soapSearch.search";
}

internal sealed record SoapSearchCommandResult(string CommandId, SoapSearchResult Value)
    : RelayCommandResult(CommandId),
        IWireRecord
{
    public static string Kind => "soapSearch.search.result";
}
