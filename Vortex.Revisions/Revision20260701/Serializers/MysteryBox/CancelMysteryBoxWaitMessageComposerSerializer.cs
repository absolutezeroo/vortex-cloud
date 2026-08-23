using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Mysterybox;

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
