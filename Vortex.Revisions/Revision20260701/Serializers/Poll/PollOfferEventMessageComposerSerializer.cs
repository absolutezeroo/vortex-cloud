using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Poll;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>Id, type, headline, summary — the order the client's offer parser reads them in.</summary>
internal class PollOfferEventMessageComposerSerializer(int header)
    : AbstractSerializer<PollOfferEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PollOfferEventMessageComposer message)
    {
        packet.WriteInteger(message.PollId);
        packet.WriteString(message.PollType);
        packet.WriteString(message.Headline);
        packet.WriteString(message.Summary);
    }
}
