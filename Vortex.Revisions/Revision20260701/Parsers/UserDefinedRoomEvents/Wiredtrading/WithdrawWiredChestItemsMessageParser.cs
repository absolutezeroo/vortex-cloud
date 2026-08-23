using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Reads the chest id, then the three fields the client's <c>ChestItemType.addToComposer</c> pushes
/// in that order, then the count. The type is written by the client in the same order it reads it
/// back in <c>ChestStorage</c>, so the two ends agree without inverting anything.
/// </summary>
internal class WithdrawWiredChestItemsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new WithdrawWiredChestItemsMessage
        {
            ChestId = packet.PopInt(),
            IsWallItem = packet.PopBoolean(),
            TypeId = packet.PopInt(),
            LegacyPosterId = packet.PopString(),
            Count = packet.PopInt(),
        };
}
