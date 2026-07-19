using Vortex.Primitives.Messages.Incoming.Game.Score;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Score;

internal class Game2GetWeeklyFriendsLeaderboardMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new Game2GetWeeklyFriendsLeaderboardMessage();
}
