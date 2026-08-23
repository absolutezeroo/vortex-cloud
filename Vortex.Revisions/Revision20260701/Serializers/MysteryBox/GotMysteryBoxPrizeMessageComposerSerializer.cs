using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Mysterybox;

namespace Vortex.Revisions.Revision20260701.Serializers.MysteryBox;

internal class GotMysteryBoxPrizeMessageComposerSerializer(int header)
    : AbstractSerializer<GotMysteryBoxPrizeMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GotMysteryBoxPrizeMessageComposer message
    )
    {
        packet.WriteString(message.ContentType).WriteInteger(message.ClassId);
    }
}
