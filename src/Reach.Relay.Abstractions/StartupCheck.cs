namespace Reach.Relay;

public interface IStartupCheck
{
    Task Check(CancellationToken cancellationToken);
}
