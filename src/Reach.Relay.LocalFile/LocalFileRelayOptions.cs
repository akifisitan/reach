using FluentValidation;

namespace Reach.Relay.LocalFile;

public sealed class LocalFileRelayOptions
{
    public required string LocalToRemoteFilePath { get; set; }
    public required string RemoteToLocalFilePath { get; set; }
}

public sealed class LocalFileOptionsValidator : AbstractValidator<LocalFileRelayOptions>
{
    public LocalFileOptionsValidator()
    {
        RuleFor(x => x.LocalToRemoteFilePath).NotEmpty().Must(Path.Exists);
        RuleFor(x => x.RemoteToLocalFilePath).NotEmpty().Must(Path.Exists);
    }
}
