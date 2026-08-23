using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Landingview.Votes;

namespace Vortex.Revisions.Revision20260701.Parsers.LandingView.Votes;

internal class CommunityGoalVoteMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new CommunityGoalVoteMessage();
}
