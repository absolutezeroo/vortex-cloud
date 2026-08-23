using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

/// <summary>
/// Field order here is the contract with vortex-modern-client's
/// VortexFurniEditorDataMessageParser — keep the two in lockstep.
/// </summary>
internal class VortexFurniEditorDataMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFurniEditorDataMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFurniEditorDataMessageComposer message
    )
    {
        packet
            .WriteInteger(message.ObjectId)
            .WriteInteger((int)message.ProductType)
            .WriteInteger(message.DefinitionId)
            .WriteInteger(message.SpriteId)
            .WriteString(message.DefinitionName)
            .WriteInteger(message.X)
            .WriteInteger(message.Y)
            .WriteInteger(message.ZHundredths)
            .WriteInteger((int)message.Rotation)
            .WriteInteger(message.WallOffset)
            .WriteString(message.ExtraData)
            .WriteInteger(message.OwnerId)
            .WriteString(message.OwnerName)
            .WriteString(message.Error);
    }
}
