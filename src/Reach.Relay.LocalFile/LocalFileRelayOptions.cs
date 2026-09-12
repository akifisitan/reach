using FluentValidation;

namespace Reach.Relay.LocalFile;

public sealed record LocalFileRelayOptions
{
    public required string LocalToRemoteFilePath { get; init; }
    public required string RemoteToLocalFilePath { get; init; }
    public int PollDelayMs { get; init; } = 3000;

    public static LocalFileRelayOptions CreateDefault(string basePath)
    {
        var relayBasePath = Path.Combine(basePath, "localrelay");

        return new()
        {
            LocalToRemoteFilePath = Path.Combine(relayBasePath, "relay_local_to_remote.jsonl"),
            RemoteToLocalFilePath = Path.Combine(relayBasePath, "relay_remote_to_local.jsonl"),
        };
    }
}

public sealed class LocalFileOptionsValidator : AbstractValidator<LocalFileRelayOptions>
{
    public LocalFileOptionsValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.LocalToRemoteFilePath).NotEmpty();
        RuleFor(x => x.RemoteToLocalFilePath).NotEmpty();
        RuleFor(x => x.PollDelayMs).InclusiveBetween(500, 60_000);
        RuleFor(x => x.LocalToRemoteFilePath)
            .Must(x => !string.IsNullOrWhiteSpace(Path.GetExtension(x)))
            .WithMessage("Relay file path must have an extension.");
        RuleFor(x => x.RemoteToLocalFilePath)
            .Must(x => !string.IsNullOrWhiteSpace(Path.GetExtension(x)))
            .WithMessage("Relay file path must have an extension.");
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
