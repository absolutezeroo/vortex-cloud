using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.MysteryBox;

internal class ShowMysteryBoxWaitMessageComposerSerializer(int header)
    : AbstractSerializer<ShowMysteryBoxWaitMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ShowMysteryBoxWaitMessageComposer message
    )
    {
        //
    }
}
