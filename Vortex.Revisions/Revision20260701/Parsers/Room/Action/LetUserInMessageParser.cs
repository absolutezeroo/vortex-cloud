using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Action;

internal class LetUserInMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new LetUserInMessage { Username = packet.PopString(), CanEnter = packet.PopBoolean() };
}
