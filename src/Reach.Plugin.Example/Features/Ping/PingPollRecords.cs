using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Ping;

internal sealed record PingPollPayload(string CorrelationId) : RpcPayload;

internal abstract record PingPollResult : RpcResult
{
    public sealed record NotFound : PingPollResult;

    public sealed record Ongoing : PingPollResult;

    public sealed record Success(string Reply) : PingPollResult;
}
