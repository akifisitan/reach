using System.Buffers;
using System.IO.Compression;

namespace Reach.Relay;

/// <summary>
/// Applies Brotli compression before an inner binary-safe text encoding, such as Base64.
/// </summary>
public sealed class BrotliCompressedRelayTextEncoding : IRelayTextEncoding
{
    private readonly IRelayTextEncoding _inner;
    private readonly int _maxDecodedPayloadBytes;

    public BrotliCompressedRelayTextEncoding(
        IRelayTextEncoding inner,
        int maxDecodedPayloadBytes = 16 * 1024 * 1024
    )
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDecodedPayloadBytes);
        _inner = inner;
        _maxDecodedPayloadBytes = maxDecodedPayloadBytes;
    }

    public string Encode(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > _maxDecodedPayloadBytes)
        {
            throw new InvalidDataException("Payload exceeds the decoded size limit.");
        }

        using var output = new MemoryStream();
        using (
            var compressor = new BrotliStream(
                output,
                CompressionLevel.SmallestSize,
                leaveOpen: true
            )
        )
        {
            compressor.Write(payload);
        }

        return _inner.Encode(output.GetBuffer().AsSpan(0, (int)output.Length));
    }

    public byte[] Decode(string text)
    {
        var compressed = _inner.Decode(text);
        var remaining = compressed.AsSpan();
        var buffer = new byte[Math.Min(81920, _maxDecodedPayloadBytes)];
        using var output = new MemoryStream();
        using var decoder = new BrotliDecoder();

        while (true)
        {
            var status = decoder.Decompress(remaining, buffer, out var consumed, out var written);
            remaining = remaining[consumed..];

            if (output.Length + written > _maxDecodedPayloadBytes)
            {
                throw new InvalidDataException("Payload exceeds the decoded size limit.");
            }

            output.Write(buffer.AsSpan(0, written));

            if (status == OperationStatus.Done && remaining.IsEmpty)
            {
                return output.ToArray();
            }

            // Require a complete single Brotli stream; truncated data must not be accepted.
            if (status != OperationStatus.DestinationTooSmall)
            {
                throw new InvalidDataException("Payload is not a complete single Brotli stream.");
            }
        }
    }
}
