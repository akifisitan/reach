using FluentValidation;

namespace Reach.Relay.Confluence;

public sealed record ConfluenceOptions
{
    public const string SectionName = "Confluence";

    public required string BaseUrl { get; set; }
    public required long LocalToRemotePageId { get; set; }
    public required long RemoteToLocalPageId { get; set; }
    public required int PollDelayMs { get; set; }
    public required int BatchLimit { get; set; }
}

public sealed class ConfluenceOptionsValidator : AbstractValidator<ConfluenceOptions>
{
    public ConfluenceOptionsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute));
        RuleFor(x => x.LocalToRemotePageId).GreaterThan(0);
        RuleFor(x => x.RemoteToLocalPageId).GreaterThan(0);
        RuleFor(x => x.PollDelayMs).InclusiveBetween(500, 60_000);
        RuleFor(x => x.BatchLimit).InclusiveBetween(1, 100);
    }
}
