using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Withdraws this player's own open reports. Their client sends it when they choose to replace what
/// they already filed, and submits the new report the moment the acknowledgement comes back — so
/// the reply is what unblocks them, not a courtesy.
/// </summary>
public class DeletePendingCallsForHelpMessageHandler(IGrainFactory grainFactory)
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

        // Via the queue grain so a withdrawn report also leaves the moderators' lists, instead of
        // sitting there until one of them picks a ticket that no longer exists.
        await grainFactory
            .GetModerationQueueGrain()
            .WithdrawForReporterAsync(PlayerId.Parse(ctx.PlayerId), ct)
            .ConfigureAwait(false);

        // Sent whatever the count: the client is waiting on this to continue, and "you had none to
        // withdraw" is not a reason to leave it waiting.
        await ctx.SendComposerAsync(new CallForHelpPendingCallsDeletedMessageComposer(), ct)
            .ConfigureAwait(false);
    }
}
