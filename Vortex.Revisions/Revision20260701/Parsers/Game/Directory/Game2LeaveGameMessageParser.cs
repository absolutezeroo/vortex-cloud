using Vortex.Primitives.Messages.Incoming.Game.Directory;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Directory;

internal class Game2LeaveGameMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new Game2LeaveGameMessage();
}
