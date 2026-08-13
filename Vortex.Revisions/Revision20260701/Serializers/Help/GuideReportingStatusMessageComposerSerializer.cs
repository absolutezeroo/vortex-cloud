using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideReportingStatusMessageComposerSerializer(int header)
    : AbstractSerializer<GuideReportingStatusMessageComposer>(header)
{
    /// <summary>Status 1, and only status 1, is followed by the ticket block.</summary>
    private const int PendingTicketStatus = 1;

    /// <summary>Nothing pending — the status a missing ticket falls back to.</summary>
    private const int NothingPendingStatus = 0;

    protected override void Serialize(
        IServerPacket packet,
        GuideReportingStatusMessageComposer message
    )
    {
        bool hasTicket =
            message.StatusCode == PendingTicketStatus && message.PendingTicket is not null;

        // Status 1 promises a struct. With none to write it is downgraded to "nothing pending"
        // rather than sent as a promise the body does not keep — the client would otherwise parse
        // whatever follows in the buffer as a ticket, and the window it opens is the wrong one
        // either way.
        packet.WriteInteger(
            message.StatusCode == PendingTicketStatus && !hasTicket
                ? NothingPendingStatus
                : message.StatusCode
        );

        if (!hasTicket)
        {
            return;
        }

        GuidePendingTicket ticket = message.PendingTicket!;

        packet
            .WriteInteger(ticket.TicketType)
            .WriteInteger(ticket.SecondsAgo)
            .WriteBoolean(ticket.IsGuide);

        switch (ticket.TicketType)
        {
            case 0:
            case 2:
                packet.WriteString(ticket.OtherPartyName).WriteString(ticket.OtherPartyFigure);
                break;

            case 1:
                packet
                    .WriteString(ticket.OtherPartyName)
                    .WriteString(ticket.OtherPartyFigure)
                    .WriteString(ticket.Description);
                break;

            case 3:
                // The odd one: for type 3 the client reads these three only when isGuide is false.
                // Writing them to a guide desynchronises everything after this message, and it is
                // the boolean three fields earlier that decides — not anything about the strings.
                if (!ticket.IsGuide)
                {
                    packet
                        .WriteString(ticket.OtherPartyName)
                        .WriteString(ticket.OtherPartyFigure)
                        .WriteString(ticket.RoomName);
                }

                break;

            default:
                // Every other type ends after the boolean. Not an error: the client's switch simply
                // returns, so anything written here would be read as the next message.
                break;
        }
    }
}
