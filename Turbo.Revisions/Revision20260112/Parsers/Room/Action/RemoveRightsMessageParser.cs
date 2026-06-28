using System.Collections.Immutable;
using Turbo.Primitives.Messages.Incoming.Room.Action;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Room.Action;

internal class RemoveRightsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int count = packet.PopInt();
        ImmutableArray<int>.Builder ids = ImmutableArray.CreateBuilder<int>(count);

        for (int i = 0; i < count; i++)
        {
            ids.Add(packet.PopInt());
        }

        return new RemoveRightsMessage { TargetUserIds = ids.ToImmutable() };
    }
}
