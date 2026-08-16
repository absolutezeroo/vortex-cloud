using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Vortex.LoadGen;

/// <summary>Builds one frame, big-endian, with the length filled in at the end.</summary>
public sealed class BenchmarkPacketWriter(int header)
{
    private readonly MemoryStream _body = new();

    public BenchmarkPacketWriter Int(int value)
    {
        Span<byte> buffer = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        _body.Write(buffer);

        return this;
    }

    public BenchmarkPacketWriter String(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)bytes.Length);
        _body.Write(length);
        _body.Write(bytes);

        return this;
    }

    public byte[] ToArray()
    {
        byte[] body = _body.ToArray();
        byte[] frame = new byte[body.Length + 6];

        BinaryPrimitives.WriteInt32BigEndian(frame, body.Length + 2);
        BinaryPrimitives.WriteInt16BigEndian(frame.AsSpan(4), (short)header);
        body.CopyTo(frame, 6);

        return frame;
    }
}
