using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Poll;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class PollMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.PollAnswerEvent, new PollAnswerMessageParser());
        builder.MapParser(MessageEvent.PollRejectEvent, new PollRejectMessageParser());
        builder.MapParser(MessageEvent.PollStartEvent, new PollStartMessageParser());
    }
}
