using Vortex.Primitives.Messages.Incoming.Notifications;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Notifications;

internal class ResetUnseenItemsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new ResetUnseenItemsMessage();
}
