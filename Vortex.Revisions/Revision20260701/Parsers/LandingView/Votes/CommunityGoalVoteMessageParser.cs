using Vortex.Primitives.Messages.Incoming.Landingview.Votes;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.LandingView.Votes;

internal class CommunityGoalVoteMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CommunityGoalVoteMessage();
}
