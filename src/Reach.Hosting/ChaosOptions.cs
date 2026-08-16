using FluentValidation;

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

public sealed class ChaosOptionsValidator : AbstractValidator<ChaosOptions>
{
    public ChaosOptionsValidator()
    {
        RuleFor(x => x.Retry).SetValidator(new ChaosPointOptionsValidator());
        RuleFor(x => x.RpcTransport).SetValidator(new ChaosPointOptionsValidator());
        RuleFor(x => x.RelayPost).SetValidator(new ChaosPointOptionsValidator());
        RuleFor(x => x.RelayResult).SetValidator(new ChaosPointOptionsValidator());
        RuleFor(x => x.RelayPoll).SetValidator(new ChaosPollOptionsValidator());
    }
}

public class ChaosPointOptions
{
    public bool Enabled { get; set; }
    public double InjectionRate { get; set; }
}

public class ChaosPointOptionsValidator : AbstractValidator<ChaosPointOptions>
{
    public ChaosPointOptionsValidator()
    {
        RuleFor(x => x.InjectionRate).InclusiveBetween(0, 1);
    }
}

public sealed class ChaosPollOptions : ChaosPointOptions
{
    public bool LatencyEnabled { get; set; }
    public double LatencyRate { get; set; }
    public TimeSpan Latency { get; set; } = TimeSpan.FromSeconds(2);
}

public sealed class ChaosPollOptionsValidator : AbstractValidator<ChaosPollOptions>
{
    public ChaosPollOptionsValidator()
    {
        Include(new ChaosPointOptionsValidator());
        RuleFor(x => x.LatencyRate).InclusiveBetween(0, 1);
        RuleFor(x => x.Latency).GreaterThan(TimeSpan.Zero).When(x => x.LatencyEnabled);
    }
}

public sealed class ChaosFaultException(string pointName)
    : Exception($"Chaos fault injected at '{pointName}'.")
{
    public string PointName { get; } = pointName;
}
