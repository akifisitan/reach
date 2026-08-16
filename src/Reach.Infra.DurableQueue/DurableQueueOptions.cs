using FluentValidation;

namespace Reach.Infra.DurableQueue;

public sealed record DurableQueueOptions
{
    public int MaxTryCount { get; set; }
    public required string TableName { get; set; }
}

public sealed class DurableQueueOptionsValidator : AbstractValidator<DurableQueueOptions>
{
    public DurableQueueOptionsValidator()
    {
        RuleFor(x => x.MaxTryCount).GreaterThan(0);
        RuleFor(x => x.TableName).NotEmpty();
    }
}
