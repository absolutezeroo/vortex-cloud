using Vortex.Primitives.Messages.Incoming.Moderator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Moderator;

internal class GetCfhChatlogMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetCfhChatlogMessage { CallId = packet.PopInt() };
}
