using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Moderation;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Lists the reports this player already has open, which their client asks for before letting them
/// file another.
/// </summary>
/// <remarks>
/// Answering matters more than it looks: the client treats an empty list as permission to proceed
/// and a non-empty one as "you already told us". Unanswered, the report dialog waited on a packet
/// that never came — so the reply is sent even when there is nothing pending, and especially then.
/// </remarks>
public class GetPendingCallsForHelpMessageHandler(ICfhTicketService tickets)
    : IMessageHandler<GetPendingCallsForHelpMessage>
{
    public async ValueTask HandleAsync(
        GetPendingCallsForHelpMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<CfhPendingCallSnapshot> calls = await tickets
            .GetPendingForReporterAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CallForHelpPendingCallsMessageComposer { Calls = calls },
                ct
            )
            .ConfigureAwait(false);
    }
}
