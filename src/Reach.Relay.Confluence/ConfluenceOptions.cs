using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace Reach.Relay.Confluence;

public sealed record ConfluenceOptions
{
    public required string BaseUrl { get; init; }
    public required long LocalToRemotePageId { get; init; }
    public required long RemoteToLocalPageId { get; init; }
    public int PollDelayMs { get; init; } = 5000;
    public int BatchLimit { get; init; } = 100;

    public static ConfluenceOptions Empty { get; } =
        new()
        {
            BaseUrl = string.Empty,
            LocalToRemotePageId = 0,
            RemoteToLocalPageId = 0,
        };

    internal static bool TryNormalizeBaseUrl(
        [NotNullWhen(true)] string? value,
        [NotNullWhen(true)] out string? normalized
    )
    {
        normalized = null;

        if (
            !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo)
        )
        {
            return false;
        }

        normalized = $"{uri.GetLeftPart(UriPartial.Authority).TrimEnd('/')}/";
        return true;
    }

    internal static string NormalizeBaseUrl(string value) =>
        TryNormalizeBaseUrl(value, out var normalized)
            ? normalized
            : throw new InvalidOperationException("The Confluence base URL is invalid.");
}

public sealed class ConfluenceOptionsValidator : AbstractValidator<ConfluenceOptions>
{
    public ConfluenceOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(x => ConfluenceOptions.TryNormalizeBaseUrl(x, out _))
            .WithMessage("must be an HTTP or HTTPS origin without a path, query, or fragment.");
        RuleFor(x => x.LocalToRemotePageId).GreaterThan(0);
        RuleFor(x => x.RemoteToLocalPageId).GreaterThan(0);
        RuleFor(x => x.RemoteToLocalPageId).NotEqual(x => x.LocalToRemotePageId);
        RuleFor(x => x.PollDelayMs).InclusiveBetween(500, 60_000);
        RuleFor(x => x.BatchLimit).InclusiveBetween(1, 100);
    }
}
