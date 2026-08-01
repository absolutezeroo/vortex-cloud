using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.LandingView;
using Vortex.Revisions.Revision20260701.Parsers.LandingView.Votes;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class LandingViewMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.CommunityGoalVoteMessageEvent,
            new CommunityGoalVoteMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetPromoArticlesMessageEvent,
            new GetPromoArticlesMessageParser()
        );
    }
}
