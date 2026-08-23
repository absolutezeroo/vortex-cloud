using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Incoming.Users;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class GetExtendedProfileMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetExtendedProfileMessage { UserId = (PlayerId)packet.PopInt() };
}
