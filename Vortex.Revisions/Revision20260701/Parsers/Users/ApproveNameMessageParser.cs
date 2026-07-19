using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class ApproveNameMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ApproveNameMessage { Name = packet.PopString(), Mode = packet.PopInt() };
}
