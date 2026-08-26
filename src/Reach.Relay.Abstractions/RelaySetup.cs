namespace Reach.Relay;

public interface IRelaySetup
{
    string Name { get; }
    Task<int> Run(CancellationToken cancellationToken);
}

public interface IRelayRegistration
{
    string Name { get; }
}

public sealed record RelayRegistration(string Name) : IRelayRegistration;

public sealed class RelayConfigurationException : Exception
{
    public RelayConfigurationException(string message)
        : base(message) { }

    public RelayConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed class RelaySetupRequiredException : Exception
{
    public RelaySetupRequiredException(string message)
        : base(message) { }

    public RelaySetupRequiredException(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed class RelayUnavailableException : Exception
{
    public RelayUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
