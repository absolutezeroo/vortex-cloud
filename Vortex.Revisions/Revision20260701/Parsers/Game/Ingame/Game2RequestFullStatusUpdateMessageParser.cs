using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Game.Ingame;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Ingame;

internal class Game2RequestFullStatusUpdateMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new Game2RequestFullStatusUpdateMessage();
}
