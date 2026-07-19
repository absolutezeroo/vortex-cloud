using Vortex.Primitives.Messages.Incoming.Game.Ingame;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Ingame;

internal class Game2SetUserMoveTargetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new Game2SetUserMoveTargetMessage();
}
