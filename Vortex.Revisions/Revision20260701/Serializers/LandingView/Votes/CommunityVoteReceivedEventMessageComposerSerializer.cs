using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Landingview.Votes;

namespace Vortex.Revisions.Revision20260701.Serializers.LandingView.Votes;

internal class CommunityVoteReceivedEventMessageComposerSerializer(int header)
    : AbstractSerializer<CommunityVoteReceivedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CommunityVoteReceivedEventMessageComposer message
    )
    {
        //
    }
}
