using Reach.Relay;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed record EchoCliCommandPayload(string Message) : CliCommandPayload;

internal sealed record EchoCliCommandResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record EchoPayload(string CorrelationId, string Message) : RpcPayload;

internal abstract record EchoResult : RpcResult
{
    public sealed record Success(string CorrelationId) : EchoResult;

    public sealed record Error(string Message) : EchoResult;
}

internal sealed record EchoCommand(string Id, string Message) : RelayCommand(Id), IWireRecord
{
    public static string Kind => "exampleEcho";
}

internal sealed record EchoCommandResult(string Reply, string CommandId)
    : RelayCommandResult(Guid.NewGuid().ToString("N"), CommandId),
        IWireRecord
{
    public static string Kind => "exampleEcho.result";
}
