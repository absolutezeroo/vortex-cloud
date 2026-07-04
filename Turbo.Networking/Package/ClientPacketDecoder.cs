using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using Turbo.Networking.Configuration;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Networking.Package;

internal sealed class ClientPacketDecoder(
    int maxPacketBodyBytes = NetworkingConfig.DefaultMaxPacketBodyBytes
) : IClientPacketDecoder
{
    private readonly int _maxPacketBodyBytes = maxPacketBodyBytes;

    public IClientPacket TryRead(ref SequenceReader<byte> reader, ISessionContext ctx)
    {
        if (reader.Remaining < 4)
        {
            return null!;
        }

        Span<byte> hdr = stackalloc byte[4];
        reader.Sequence.Slice(reader.Consumed, 4).CopyTo(hdr);

        int length = BinaryPrimitives.ReadInt32BigEndian(
            ctx.CryptoIn is not null ? ctx.CryptoIn.Peek(hdr.ToArray()) : hdr
        );

        if (length < 0 || length > _maxPacketBodyBytes)
        {
            throw new InvalidDataException(
                $"Client packet declared an invalid body length of {length} bytes "
                    + $"(max {_maxPacketBodyBytes}) for session {ctx.SessionKey}."
            );
        }

        if (reader.Remaining < (length + 4))
        {
            return null!;
        }

        byte[] unread = reader.Sequence.Slice(reader.Consumed, length + 4).ToArray();
        byte[] body = ctx.CryptoIn is not null ? ctx.CryptoIn.Process(unread) : unread;
        ClientPacket packet = new ClientPacket(-1, body);

        length = packet.PopInt();
        packet.Header = packet.PopShort();

        reader.Advance(length + 4);

        return packet;
    }
}
