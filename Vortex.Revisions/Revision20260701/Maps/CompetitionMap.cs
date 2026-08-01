using Vortex.Primitives.Messages.Outgoing.Competition;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Competition;
using Vortex.Revisions.Revision20260701.Serializers.Competition;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class CompetitionMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.ForwardToACompetitionRoomMessageEvent,
            new ForwardToACompetitionRoomMessageParser()
        );
        builder.MapParser(
            MessageEvent.ForwardToASubmittableRoomMessageEvent,
            new ForwardToASubmittableRoomMessageParser()
        );
        builder.MapParser(
            MessageEvent.ForwardToRandomCompetitionRoomMessageEvent,
            new ForwardToRandomCompetitionRoomMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetCurrentTimingCodeMessageEvent,
            new GetCurrentTimingCodeMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetIsUserPartOfCompetitionMessageEvent,
            new GetIsUserPartOfCompetitionMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetSecondsUntilMessageEvent,
            new GetSecondsUntilMessageParser()
        );
        builder.MapParser(
            MessageEvent.RoomCompetitionInitMessageEvent,
            new RoomCompetitionInitMessageParser()
        );
        builder.MapParser(
            MessageEvent.SubmitRoomToCompetitionMessageEvent,
            new SubmitRoomToCompetitionMessageParser()
        );
        builder.MapParser(MessageEvent.VoteForRoomMessageEvent, new VoteForRoomMessageParser());

        builder.MapSerializer(
            typeof(CurrentTimingCodeMessageComposer),
            new CurrentTimingCodeMessageComposerSerializer(
                MessageComposer.CurrentTimingCodeMessageComposer
            )
        );
    }
}
