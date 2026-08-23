using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Engine;

internal class VortexGetFurniDefinitionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new VortexGetFurniDefinitionMessage { DefinitionId = packet.PopInt() };
}

/// <summary>
/// Field order is the contract with vortex-modern-client's VortexApplyFurniDefinitionComposer, and
/// is deliberately identical to VortexFurniDefinitionMessageComposerSerializer's (minus the trailing
/// error string) so the round trip reads the same way in both directions.
/// </summary>
internal class VortexApplyFurniDefinitionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new VortexApplyFurniDefinitionMessage
        {
            DefinitionId = packet.PopInt(),
            SpriteId = packet.PopInt(),
            Name = packet.PopString(),
            ProductType = (ProductType)packet.PopInt(),
            FurniCategory = (FurnitureCategory)packet.PopInt(),
            Logic = packet.PopString(),
            TotalStates = packet.PopInt(),
            Width = packet.PopInt(),
            Length = packet.PopInt(),
            StackHeightHundredths = packet.PopInt(),
            CanStack = packet.PopBoolean(),
            CanWalk = packet.PopBoolean(),
            CanSit = packet.PopBoolean(),
            CanLay = packet.PopBoolean(),
            CanRecycle = packet.PopBoolean(),
            CanTrade = packet.PopBoolean(),
            CanGroup = packet.PopBoolean(),
            CanSell = packet.PopBoolean(),
            UsagePolicy = (FurnitureUsageType)packet.PopInt(),
            ExtraData = packet.PopString(),
            StuffDataType = (StuffDataType)packet.PopInt(),
        };
}
