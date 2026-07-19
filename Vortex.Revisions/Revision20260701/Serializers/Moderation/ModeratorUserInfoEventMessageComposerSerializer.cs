using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorUserInfoEventMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorUserInfoEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModeratorUserInfoEventMessageComposer message
    )
    {
        //
    }
}
