using FluentValidation;

namespace Reach.Relay.Confluence;

public sealed record ConfluenceOptions
{
    public required string BaseUrl { get; set; }
    public required long LocalToRemotePageId { get; set; }
    public required long RemoteToLocalPageId { get; set; }
    public required int PollDelayMs { get; set; }
    public required int Count { get; set; }
}

public sealed class ConfluenceOptionsValidator : AbstractValidator<ConfluenceOptions>
{
    public ConfluenceOptionsValidator()
    {
        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute));
        RuleFor(x => x.LocalToRemotePageId).ExclusiveBetween(0, 99999999);
        RuleFor(x => x.RemoteToLocalPageId).ExclusiveBetween(0, 99999999);
        RuleFor(x => x.PollDelayMs).ExclusiveBetween(500, 10 * 1000);
        RuleFor(x => x.Count).ExclusiveBetween(5, 100);
    }
}
