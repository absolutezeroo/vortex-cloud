using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;

internal class ApplySnapshotMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ApplySnapshotMessage { Id = packet.PopInt() };
}
