using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class HabboUserBadgesMessageComposerSerializer(int header)
    : AbstractSerializer<HabboUserBadgesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabboUserBadgesMessageComposer message)
    {
        //
    }
}
