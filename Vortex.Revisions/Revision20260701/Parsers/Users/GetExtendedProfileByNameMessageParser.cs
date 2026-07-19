using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class GetExtendedProfileByNameMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetExtendedProfileByNameMessage { UserName = packet.PopString() };
}
