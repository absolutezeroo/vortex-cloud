using Vortex.Protocol.Messages.Incoming.Preferences;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

internal class AddToCustomFilterMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new AddToCustomFilterMessage { Word = packet.PopString() };
}
