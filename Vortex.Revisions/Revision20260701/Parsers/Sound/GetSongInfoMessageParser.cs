using System;
using System.Collections.Immutable;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Sound;

namespace Vortex.Revisions.Revision20260701.Parsers.Sound;

internal class GetSongInfoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        // The count comes from the client, and the ids behind it are four bytes each: what is left
        // in the packet is the only honest ceiling. Sizing the builder from the declared count alone
        // lets one malformed packet ask for an arbitrarily large allocation before a single read
        // fails.
        int count = Math.Clamp(packet.PopInt(), 0, packet.Remaining / sizeof(int));
        ImmutableArray<int>.Builder songIds = ImmutableArray.CreateBuilder<int>(count);

        for (int i = 0; i < count; i++)
        {
            songIds.Add(packet.PopInt());
        }

        return new GetSongInfoMessage { SongIds = songIds.ToImmutable() };
    }
}
