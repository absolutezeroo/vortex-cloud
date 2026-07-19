using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class GuildBaseSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GuildBaseSearchMessage { Unknown = packet.PopInt() };
}
