using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Mysterybox;

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
