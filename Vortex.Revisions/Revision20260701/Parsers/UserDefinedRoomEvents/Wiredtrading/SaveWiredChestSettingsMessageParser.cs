using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class SaveWiredChestSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SaveWiredChestSettingsMessage
        {
            ChestId = packet.PopInt(),
            Name = packet.PopString(),
            Description = packet.PopString(),
            EveryoneCanOpen = packet.PopBoolean(),
            EveryoneCanDonate = packet.PopBoolean(),
            ChestState = packet.PopInt(),
            PreviewItems = packet.PopInt(),
            PreviewAmount = packet.PopInt(),
            UpgradeButtonDisabled = packet.PopBoolean(),
        };
}
