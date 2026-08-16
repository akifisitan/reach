using FluentValidation;

namespace Reach.Local.Server;

internal sealed record AppOptions
{
    public const string SectionName = "AppOptions";

    public bool DebugMode { get; set; }
}

internal sealed record ServerExecutorOptions
{
    public const string SectionName = nameof(ServerExecutorOptions);

    public required int QueueProcessorCount { get; set; }
    public required int RetryQueueProcessorCount { get; set; }
}

internal sealed class ServerExecutorOptionsValidator : AbstractValidator<ServerExecutorOptions>
{
    public ServerExecutorOptionsValidator()
    {
        RuleFor(x => x.QueueProcessorCount).InclusiveBetween(1, 16);
        RuleFor(x => x.RetryQueueProcessorCount).InclusiveBetween(1, 16);
    }
}
