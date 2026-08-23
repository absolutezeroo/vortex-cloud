using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>No payload — the client's composer writes an empty array.</summary>
internal class WiredTradeCancelMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new WiredTradeCancelMessage();
}
