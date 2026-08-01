using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Talent;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class TalentMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.GetTalentTrackLevelMessageEvent,
            new GetTalentTrackLevelMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetTalentTrackMessageEvent,
            new GetTalentTrackMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideAdvertisementReadMessageEvent,
            new GuideAdvertisementReadMessageParser()
        );
    }
}
