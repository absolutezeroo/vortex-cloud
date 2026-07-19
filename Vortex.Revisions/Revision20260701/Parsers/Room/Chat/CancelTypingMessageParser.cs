using Vortex.Primitives.Messages.Incoming.Room.Chat;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Chat;

internal class CancelTypingMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CancelTypingMessage();
}
