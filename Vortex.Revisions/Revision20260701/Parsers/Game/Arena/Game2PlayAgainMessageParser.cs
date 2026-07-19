using Vortex.Primitives.Messages.Incoming.Game.Arena;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Arena;

internal class Game2PlayAgainMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new Game2PlayAgainMessage();
}
