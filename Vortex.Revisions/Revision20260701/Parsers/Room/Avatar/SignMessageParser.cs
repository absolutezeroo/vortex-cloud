using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class SignMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SignMessage { SignId = packet.PopInt() };
}
