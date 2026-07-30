namespace Reach.Hosting;

public static class ChaosPolicyNames
{
    public const string RelayPoll = "chaos-relay-poll";
    public const string RelayPost = "chaos-relay-post";
    public const string RelayResult = "chaos-relay-result";
    public const string RpcTransport = "chaos-rpc-transport";
}

public sealed class ChaosOptions
{
    public const string SectionName = "Chaos";

    public bool Enabled { get; set; }
    public ChaosPointOptions Retry { get; set; } = new();
    public ChaosPointOptions RpcTransport { get; set; } = new();
    public ChaosPointOptions RelayPost { get; set; } = new();
    public ChaosPointOptions RelayResult { get; set; } = new();
    public ChaosPollOptions RelayPoll { get; set; } = new();
}

public class ChaosPointOptions
{
    public bool Enabled { get; set; }
    public double InjectionRate { get; set; }
}

public sealed class ChaosPollOptions : ChaosPointOptions
{
    public bool LatencyEnabled { get; set; }
    public double LatencyRate { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.FromSeconds(2);
}

public sealed class ChaosFaultException(string pointName)
    : Exception($"Chaos fault injected at '{pointName}'.")
{
    public string PointName { get; } = pointName;
}
