namespace Reach.Relay;

public sealed class Base64RelayTextEncoding : IRelayTextEncoding
{
    public string Encode(ReadOnlySpan<byte> payload) => Convert.ToBase64String(payload);

    public byte[] Decode(string text) => Convert.FromBase64String(text);
}
