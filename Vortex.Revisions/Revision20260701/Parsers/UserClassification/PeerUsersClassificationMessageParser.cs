using Vortex.Primitives.Messages.Incoming.Userclassification;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserClassification;

internal class PeerUsersClassificationMessageParser : IParser
{
    // _SafeCls_3991(param1:String) - one string, the classification keyword.
    public IMessageEvent Parse(IClientPacket packet) =>
        new PeerUsersClassificationMessage { Classification = packet.PopString() };
}
