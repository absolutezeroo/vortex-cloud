using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Moderation;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Withdraws this player's own open reports. Their client sends it when they choose to replace what
/// they already filed, and submits the new report the moment the acknowledgement comes back — so
/// the reply is what unblocks them, not a courtesy.
/// </summary>
public class DeletePendingCallsForHelpMessageHandler(ICfhTicketService tickets)
    : IMessageHandler<DeletePendingCallsForHelpMessage>
{
    public async ValueTask HandleAsync(
        DeletePendingCallsForHelpMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await tickets.DeletePendingForReporterAsync(ctx.PlayerId, ct).ConfigureAwait(false);

        // Sent whatever the count: the client is waiting on this to continue, and "you had none to
        // withdraw" is not a reason to leave it waiting.
        await ctx.SendComposerAsync(new CallForHelpPendingCallsDeletedMessageComposer(), ct)
            .ConfigureAwait(false);
    }
}
