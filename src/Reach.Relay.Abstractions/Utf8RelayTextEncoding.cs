using System.Text;

namespace Reach.Relay;

public sealed class Utf8RelayTextEncoding : IRelayTextEncoding
{
    private static readonly Encoding Utf8 = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    public string Encode(ReadOnlySpan<byte> payload) => Utf8.GetString(payload);

    public byte[] Decode(string text) => Utf8.GetBytes(text);
}
