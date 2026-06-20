using Turbo.Primitives.Messages.Outgoing.Landingview.Votes;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.LandingView.Votes;

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
