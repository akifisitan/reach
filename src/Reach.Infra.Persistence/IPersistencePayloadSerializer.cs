namespace Reach.Infra.Persistence;

public interface IPersistencePayloadSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string payload);
}
