using Vortex.Primitives.Messages.Incoming.Game.Lobby;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Lobby;

internal class class_165MessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new class_165Message();
}
