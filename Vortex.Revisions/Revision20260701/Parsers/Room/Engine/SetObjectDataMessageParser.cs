using System;
using System.Collections.Immutable;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class SetObjectDataMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int itemId = packet.PopInt();

        // The count is the number of STRINGS, not of pairs: the client writes `map.length * 2` and
        // then pushes each key and each value. Reading it as a pair count consumes half the message
        // and leaves the rest to be read as the next packet's header.
        //
        // Clamped to what is left in the packet, because the count is the client's word: a string is
        // at least its own two-byte length prefix, so nothing honest can claim more pairs than that.
        int strings = Math.Clamp(packet.PopInt(), 0, packet.Remaining / 2);
        int pairs = strings / 2;

        ImmutableArray<(string, string)>.Builder builder = ImmutableArray.CreateBuilder<(
            string,
            string
        )>(pairs);

        for (int i = 0; i < pairs; i++)
        {
            builder.Add((packet.PopString(), packet.PopString()));
        }

        return new SetObjectDataMessage { ItemId = itemId, Pairs = builder.ToImmutable() };
    }
}
