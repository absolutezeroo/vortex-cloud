using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.MysteryBox;

internal class MysteryBoxKeysMessageComposerSerializer(int header)
    : AbstractSerializer<MysteryBoxKeysMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, MysteryBoxKeysMessageComposer message)
    {
        packet.WriteString(message.BoxColor).WriteString(message.KeyColor);
    }
}
