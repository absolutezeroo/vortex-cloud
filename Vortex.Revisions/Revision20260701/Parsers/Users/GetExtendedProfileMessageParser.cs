using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class GetExtendedProfileMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetExtendedProfileMessage { UserId = (PlayerId)packet.PopInt() };
}
