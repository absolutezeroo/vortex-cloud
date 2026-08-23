using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Preferences;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

internal class RemoveFromCustomFilterMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RemoveFromCustomFilterMessage { Word = packet.PopString() };
}
