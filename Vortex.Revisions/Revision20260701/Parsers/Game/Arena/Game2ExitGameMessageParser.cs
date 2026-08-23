using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Game.Arena;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Arena;

internal class Game2ExitGameMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new Game2ExitGameMessage();
}
