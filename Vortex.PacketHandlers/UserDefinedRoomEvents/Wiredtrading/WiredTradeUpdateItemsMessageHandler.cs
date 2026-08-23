using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Furniture going onto, or coming off, an open deposit's table.
/// </summary>
/// <remarks>
/// The answer redraws the whole table rather than sending a delta: it is the same message the
/// player-to-player trade uses, and the client rebuilds both columns from it every time.
/// </remarks>
public class WiredTradeUpdateItemsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredTradeUpdateItemsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredTradeUpdateItemsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredDepositSnapshot? deposit = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .UpdateWiredDepositItemsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.Remove,
                message.ItemIds,
                ct
            )
            .ConfigureAwait(false);

        if (deposit is null)
        {
            return;
        }

        await ctx.SendComposerAsync(WiredTradeTable.For(ctx, deposit), ct).ConfigureAwait(false);
    }
}

/// <summary>
/// The table message, built the same way from every step of a deposit.
/// </summary>
/// <remarks>
/// The room takes the second seat with no id of its own — it is not a player — and stakes nothing:
/// a deposit is one-sided, which is also what makes the client's requirement read as payment-only.
/// </remarks>
internal static class WiredTradeTable
{
    public static WiredTradeItemsUpdateMessageComposer For(
        MessageContext ctx,
        WiredDepositSnapshot deposit
    ) =>
        new()
        {
            FirstUserId = (int)ctx.PlayerId,
            FirstUserItems = deposit.Items,
            FirstUserCredits = 0,
            // The far side is the contract's: what it hands back. A plain deposit leaves it empty,
            // which is what a chest gives you for filling it.
            SecondUserId = 0,
            SecondUserItems = deposit.RewardItems,
            SecondUserCredits = deposit.RewardCredits,
            CanAccept = deposit.CanAccept,
            Extra = 0,
        };
}
