using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.MysteryBox;

internal class CancelMysteryBoxWaitMessageComposerSerializer(int header)
    : AbstractSerializer<CancelMysteryBoxWaitMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CancelMysteryBoxWaitMessageComposer message
    )
    {
        //
    }
}
