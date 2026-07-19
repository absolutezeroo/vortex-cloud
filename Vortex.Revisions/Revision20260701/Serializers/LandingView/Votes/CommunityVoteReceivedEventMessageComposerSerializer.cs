using Vortex.Primitives.Messages.Outgoing.Landingview.Votes;
using Vortex.Primitives.Packets;

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
