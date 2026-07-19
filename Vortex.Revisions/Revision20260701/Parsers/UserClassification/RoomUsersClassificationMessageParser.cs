using Vortex.Primitives.Messages.Incoming.Userclassification;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.UserClassification;

internal class RoomUsersClassificationMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RoomUsersClassificationMessage();
}
