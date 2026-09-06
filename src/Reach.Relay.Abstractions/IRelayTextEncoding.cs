namespace Reach.Relay;

public interface IRelayTextEncoding
{
    string Encode(ReadOnlySpan<byte> payload);
    byte[] Decode(string text);
}
