using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The player closing a deposit without completing it.
/// </summary>
/// <remarks>
/// The cancellation is echoed back rather than assumed: the client closes its screen on the
/// message, not on its own click, so a cancel the room never saw would leave the screen up.
/// Failure type 0 is the plain "cancelled" of the client's own error table.
/// </remarks>
public class WiredTradeCancelMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredTradeCancelMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    private const int CancelledByPlayer = 0;

    public async ValueTask HandleAsync(
        WiredTradeCancelMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        bool cancelled = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .CancelWiredDepositAsync(ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId), ct)
            .ConfigureAwait(false);

        if (!cancelled)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new WiredTradeCancelledMessageComposer
                {
                    TransactionFailureTypeId = CancelledByPlayer,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
