using Vortex.Primitives.Messages.Incoming.Gifts;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Gifts;

internal class class_200MessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new class_200Message();
}
