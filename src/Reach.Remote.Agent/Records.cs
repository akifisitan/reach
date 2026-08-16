using FluentValidation;

namespace Reach.Remote.Agent;

internal sealed record AppOptions
{
    public const string SectionName = "AppOptions";

    public bool DebugMode { get; set; }
}

internal sealed record ExecutorOptions
{
    public int ProcessorCount { get; set; }
    public int RetryConsumerCount { get; set; }
    public int PosterCount { get; set; }
}

internal sealed class ExecutorOptionsValidator : AbstractValidator<ExecutorOptions>
{
    public ExecutorOptionsValidator()
    {
        RuleFor(x => x.ProcessorCount).InclusiveBetween(1, 16);
        RuleFor(x => x.RetryConsumerCount).InclusiveBetween(1, 16);
        RuleFor(x => x.PosterCount).InclusiveBetween(1, 16);
    }
}
