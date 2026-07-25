using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

/// <summary>
/// Field order is the contract with vortex-modern-client's VortexFurniDefinitionMessageParser, and
/// mirrors VortexApplyFurniDefinitionMessageParser's read order with the error string appended.
/// </summary>
internal class VortexFurniDefinitionMessageComposerSerializer(int header)
    : AbstractSerializer<VortexFurniDefinitionMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        VortexFurniDefinitionMessageComposer message
    )
    {
        packet
            .WriteInteger(message.DefinitionId)
            .WriteInteger(message.SpriteId)
            .WriteString(message.Name)
            .WriteInteger((int)message.ProductType)
            .WriteInteger((int)message.FurniCategory)
            .WriteString(message.Logic)
            .WriteInteger(message.TotalStates)
            .WriteInteger(message.Width)
            .WriteInteger(message.Length)
            .WriteInteger(message.StackHeightHundredths)
            .WriteBoolean(message.CanStack)
            .WriteBoolean(message.CanWalk)
            .WriteBoolean(message.CanSit)
            .WriteBoolean(message.CanLay)
            .WriteBoolean(message.CanRecycle)
            .WriteBoolean(message.CanTrade)
            .WriteBoolean(message.CanGroup)
            .WriteBoolean(message.CanSell)
            .WriteInteger((int)message.UsagePolicy)
            .WriteString(message.ExtraData)
            .WriteInteger((int)message.StuffDataType)
            .WriteString(message.Error);
    }
}
