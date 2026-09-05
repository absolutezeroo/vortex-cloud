using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Habbicons;

namespace Vortex.Revisions.Revision20260701.Serializers.Habbicons;

/// <summary>
/// The client's <c>_SafeCls_4256.parse</c>: a count and that many (habbiconId, state) pairs, then a
/// second count and that many recent ids.
/// </summary>
internal class UserHabbiconsMessageComposerSerializer(int header)
    : AbstractSerializer<UserHabbiconsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserHabbiconsMessageComposer message)
    {
        packet.WriteInteger(message.Habbicons.Length);

        foreach (PlayerHabbiconSnapshot habbicon in message.Habbicons)
        {
            packet.WriteInteger(habbicon.HabbiconId).WriteInteger((int)habbicon.State);
        }

        packet.WriteInteger(message.RecentHabbiconIds.Length);

        foreach (int habbiconId in message.RecentHabbiconIds)
        {
            packet.WriteInteger(habbiconId);
        }
    }
}
