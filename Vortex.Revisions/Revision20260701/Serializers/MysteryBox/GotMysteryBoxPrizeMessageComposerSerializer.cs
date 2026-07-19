using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.MysteryBox;

internal class GotMysteryBoxPrizeMessageComposerSerializer(int header)
    : AbstractSerializer<GotMysteryBoxPrizeMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GotMysteryBoxPrizeMessageComposer message
    )
    {
        //
    }
}
