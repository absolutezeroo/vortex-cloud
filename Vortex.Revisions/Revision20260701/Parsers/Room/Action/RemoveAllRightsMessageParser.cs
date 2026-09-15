using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Action;

internal class RemoveAllRightsMessageParser : IParser
{
    // The room settings window names the room it is editing: RoomSettingsCtrl.as:1480 sends
    // `new _SafeCls_3226(this._flatId)`, and the composer pushes that single int. Reading nothing
    // here left the handler guessing from the session's current room, which is the wrong room
    // whenever the dialog was opened from the navigator.
    public IMessageEvent Parse(IClientPacket packet) =>
        new RemoveAllRightsMessage { RoomId = packet.PopInt() };
}
