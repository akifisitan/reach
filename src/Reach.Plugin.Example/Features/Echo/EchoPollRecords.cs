using Reach.Plugin.Abstractions;

namespace Reach.Plugin.Example.Features.Echo;

internal sealed record EchoPollPayload(string CorrelationId) : RpcPayload;

internal abstract record EchoPollResult : RpcResult
{
    public sealed record NotFound : EchoPollResult;

    public sealed record Ongoing : EchoPollResult;

    public sealed record Success(string Reply) : EchoPollResult;
}
