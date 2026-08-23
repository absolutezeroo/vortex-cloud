using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

/// <summary>
/// Field order here is the contract with vortex-modern-client's VortexApplyFurniEditComposer. Both
/// sides write every field regardless of the mask, so the two orderings must stay identical or the
/// read runs off the end of the buffer.
/// </summary>
internal class VortexApplyFurniEditMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new VortexApplyFurniEditMessage
        {
            ObjectId = new RoomObjectId(packet.PopInt()),
            Fields = (FurniEditField)packet.PopInt(),
            X = packet.PopInt(),
            Y = packet.PopInt(),
            ZHundredths = packet.PopInt(),
            Rotation = (Rotation)packet.PopInt(),
            WallOffset = packet.PopInt(),
            ExtraData = packet.PopString(),
            OwnerName = packet.PopString(),
            DefinitionId = packet.PopInt(),
        };
}
