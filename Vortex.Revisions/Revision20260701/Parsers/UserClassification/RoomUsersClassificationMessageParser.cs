using Vortex.Protocol.Messages.Incoming.Userclassification;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserClassification;

internal class RoomUsersClassificationMessageParser : IParser
{
    // _SafeCls_3149(param1:String) - one string, the classification keyword.
    public IMessageEvent Parse(IClientPacket packet) =>
        new RoomUsersClassificationMessage { Classification = packet.PopString() };
}
