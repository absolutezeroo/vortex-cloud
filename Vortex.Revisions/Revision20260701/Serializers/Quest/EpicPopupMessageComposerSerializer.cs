using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Quest;

internal class EpicPopupMessageComposerSerializer(int header)
    : AbstractSerializer<EpicPopupMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, EpicPopupMessageComposer message)
    {
        //
    }
}
