using Reach.Plugin.Abstractions;
using Reach.Relay.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed record PingCliCommandPayload(string Message) : CliCommandPayload;

internal sealed record PingCliCommandResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record PingPayload(string CorrelationId, string Message) : RpcPayload;

internal abstract record PingResult : RpcResult
{
    public sealed record Success(string CorrelationId) : PingResult;

    public sealed record Error(string Message) : PingResult;
}

internal sealed record PingCommand(string Id, string Message) : RelayCommand(Id), IWireRecord
{
    public static string Kind => "examplePing";
}

internal sealed record PingCommandResult(string Reply, string CommandId)
    : RelayCommandResult(Guid.NewGuid().ToString("N"), CommandId),
        IWireRecord
{
    public static string Kind => "examplePing.result";
}
