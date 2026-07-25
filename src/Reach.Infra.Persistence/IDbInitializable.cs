namespace Reach.Infra.Persistence;

public interface IDbInitializable
{
    void EnsureCreated();
}
