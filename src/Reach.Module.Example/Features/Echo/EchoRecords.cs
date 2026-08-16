using Reach.Relay;

namespace Reach.Module.Example.Features.Echo;

internal sealed record EchoCliCommandPayload(string Message, int PollMs) : CliCommandPayload;

internal sealed record EchoCliCommandResult(int StatusCode) : CliCommandResult(StatusCode);

internal sealed record EchoPayload(string Message) : RpcPayload;

internal sealed record EchoCommand(string Id, string Message) : RelayCommand(Id), IWireRecord
{
    public static string Kind => "exampleEcho";
}

internal sealed record EchoCommandResult(string CommandId, string Reply)
    : RelayCommandResult(CommandId),
        IWireRecord
{
    public static string Kind => "exampleEcho.result";
}
