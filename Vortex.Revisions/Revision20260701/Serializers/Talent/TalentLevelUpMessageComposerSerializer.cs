using Vortex.Primitives.Messages.Outgoing.Talent;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Talent;

internal class TalentLevelUpMessageComposerSerializer(int header)
    : AbstractSerializer<TalentLevelUpMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TalentLevelUpMessageComposer message)
    {
        //
    }
}
