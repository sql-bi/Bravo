using System;
using System.Buffers.Binary;

namespace Bravo.Platform.Windows.SingleInstance;

/// <summary>
/// A request on the pipe: the version of the protocol and the length of the payload, each a little-endian
/// 32-bit integer, then the payload.
/// </summary>
internal static class HandOverFrame
{
    public const int Version = 1;

    public const int HeaderLength = 2 * sizeof(int);

    public static byte[] Create(int version, ReadOnlySpan<byte> payload)
    {
        var frame = new byte[HeaderLength + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, version);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(sizeof(int)), payload.Length);
        payload.CopyTo(frame.AsSpan(HeaderLength));
        return frame;
    }

    public static (int Version, int PayloadLength) ReadHeader(ReadOnlySpan<byte> header)
    {
        var version = BinaryPrimitives.ReadInt32LittleEndian(header);
        var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header[sizeof(int)..]);
        return (version, payloadLength);
    }
}
