using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Events;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// The requester's verdict on a finished session.
/// </summary>
/// <remarks>
/// Published rather than stored against the session, because by the time this arrives there is no
/// session left: the client only shows the feedback form once it has been told the session ended,
/// so the pairing is already gone. The rating stands on its own and goes where every other
/// moderation-side fact goes.
/// </remarks>
public class GuideSessionFeedbackMessageHandler(IEventPublisher events)
    : IMessageHandler<GuideSessionFeedbackMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionFeedbackMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await events
            .PublishAsync(new GuideSessionRatedEvent(ctx.PlayerId, message.WasHelpful), ct)
            .ConfigureAwait(false);
    }
}
