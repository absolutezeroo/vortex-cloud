using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Game.Score;

namespace Vortex.Revisions.Revision20260701.Parsers.Game.Score;

internal class GetFriendsWeeklyCompetitiveLeaderboardMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetFriendsWeeklyCompetitiveLeaderboardMessage();
}
