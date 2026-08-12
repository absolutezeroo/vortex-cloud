using Vortex.Primitives.Messages.Outgoing.Poll;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Poll;
using Vortex.Revisions.Revision20260701.Serializers.Poll;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class PollMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.PollAnswerEvent, new PollAnswerMessageParser());
        builder.MapParser(MessageEvent.PollRejectEvent, new PollRejectMessageParser());
        builder.MapParser(MessageEvent.PollStartEvent, new PollStartMessageParser());

        builder.MapSerializer(
            typeof(PollOfferEventMessageComposer),
            new PollOfferEventMessageComposerSerializer(MessageComposer.PollOfferComposer)
        );
        builder.MapSerializer(
            typeof(PollContentsEventMessageComposer),
            new PollContentsEventMessageComposerSerializer(MessageComposer.PollContentsComposer)
        );
        builder.MapSerializer(
            typeof(PollErrorEventMessageComposer),
            new PollErrorEventMessageComposerSerializer(MessageComposer.PollErrorComposer)
        );
        builder.MapSerializer(
            typeof(QuestionEventMessageComposer),
            new QuestionEventMessageComposerSerializer(MessageComposer.QuestionComposer)
        );
        builder.MapSerializer(
            typeof(QuestionAnsweredEventMessageComposer),
            new QuestionAnsweredEventMessageComposerSerializer(
                MessageComposer.QuestionAnsweredComposer
            )
        );
        builder.MapSerializer(
            typeof(QuestionFinishedEventMessageComposer),
            new QuestionFinishedEventMessageComposerSerializer(
                MessageComposer.QuestionFinishedComposer
            )
        );
    }
}
