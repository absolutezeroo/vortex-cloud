using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Game.Directory;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Directory;

internal class Game2GetAccountGameStatusMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new Game2GetAccountGameStatusMessage();
}
