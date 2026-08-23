using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Game.Score;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Score;

internal class Game2GetWeeklyFriendsLeaderboardMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new Game2GetWeeklyFriendsLeaderboardMessage();
}
