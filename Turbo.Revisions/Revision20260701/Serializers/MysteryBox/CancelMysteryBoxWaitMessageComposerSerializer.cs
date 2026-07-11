using Turbo.Primitives.Messages.Outgoing.Mysterybox;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.MysteryBox;

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
