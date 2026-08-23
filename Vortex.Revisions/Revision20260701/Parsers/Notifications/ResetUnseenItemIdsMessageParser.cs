using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Notifications;

namespace Vortex.Revisions.Revision20260701.Parsers.Notifications;

internal class ResetUnseenItemIdsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new ResetUnseenItemIdsMessage();
}
