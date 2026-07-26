namespace Reach.Relay;

public interface IRelaySetup
{
    string Name { get; }

    bool IsComplete(string basePath);

    Task<int> RunAsync(string basePath, CancellationToken cancellationToken);
}

public sealed class RelaySetupRequiredException : Exception
{
    public RelaySetupRequiredException(string message)
        : base(message) { }

    public RelaySetupRequiredException(string message, Exception innerException)
        : base(message, innerException) { }
}
