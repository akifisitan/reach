namespace Reach.Module;

public interface IRpcPayloadSerializer
{
    string Serialize<T>(T payload);
    T Deserialize<T>(string value);
}
