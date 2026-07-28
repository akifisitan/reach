namespace Reach.Infra.DurableQueue;

public interface IDurableQueueDataSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string payload);
}
