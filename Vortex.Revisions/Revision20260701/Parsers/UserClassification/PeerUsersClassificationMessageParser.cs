using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userclassification;

namespace Vortex.Revisions.Revision20260701.Parsers.UserClassification;

internal class PeerUsersClassificationMessageParser : IParser
{
    // _SafeCls_3991(param1:String) - one string, the classification keyword.
    public IMessageEvent Parse(IClientPacket packet) =>
        new PeerUsersClassificationMessage { Classification = packet.PopString() };
}
