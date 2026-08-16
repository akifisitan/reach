using FluentValidation;

namespace Reach.Relay.LocalFile;

public sealed class LocalFileRelayOptions
{
    public required string LocalToRemoteFilePath { get; set; }
    public required string RemoteToLocalFilePath { get; set; }
    public required int PollDelayMs { get; set; }
}

public sealed class LocalFileOptionsValidator : AbstractValidator<LocalFileRelayOptions>
{
    public LocalFileOptionsValidator()
    {
        RuleFor(x => x.LocalToRemoteFilePath).Must(Path.Exists);
        RuleFor(x => x.RemoteToLocalFilePath).Must(Path.Exists);
        RuleFor(x => x.PollDelayMs).InclusiveBetween(500, 60_000);
        RuleFor(x => x)
            .Must(x =>
                !string.Equals(
                    Path.GetFullPath(x.LocalToRemoteFilePath),
                    Path.GetFullPath(x.RemoteToLocalFilePath),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .WithMessage("Relay file paths must be different.");
    }
}
