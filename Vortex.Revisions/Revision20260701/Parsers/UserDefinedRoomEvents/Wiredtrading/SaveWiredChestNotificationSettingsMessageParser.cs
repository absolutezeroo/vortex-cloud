using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

internal class SaveWiredChestNotificationSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SaveWiredChestNotificationSettingsMessage
        {
            ChestId = packet.PopInt(),
            NotificationMode = packet.PopInt(),
            NotifyWhenFull = packet.PopBoolean(),
            NotifyOnDonation = packet.PopBoolean(),
            NotifyOnWithdraw = packet.PopBoolean(),
            NotifyWhenEmpty = packet.PopBoolean(),
            NotifyOnAnyWiredTransaction = packet.PopBoolean(),
        };
}
