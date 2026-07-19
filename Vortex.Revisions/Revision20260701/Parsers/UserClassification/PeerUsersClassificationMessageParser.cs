using Vortex.Primitives.Messages.Incoming.Userclassification;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserClassification;

internal class PeerUsersClassificationMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PeerUsersClassificationMessage();
}
