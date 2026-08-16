using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Taking credits out of a chest.
/// </summary>
/// <remarks>
/// The reply carries <c>isUpdate = true</c>: the screen is already open, and a false there would
/// make the client treat this as a fresh opening.
/// </remarks>
public class WithdrawWiredChestCreditsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WithdrawWiredChestCreditsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WithdrawWiredChestCreditsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.Amount <= 0)
        {
            return;
        }

        WiredChestSnapshot? chest = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .WithdrawWiredChestCreditsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.Amount,
                ct
            )
            .ConfigureAwait(false);

        if (chest is null || !chest.IsCoinChest)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new WiredChestCoinsMessageComposer
                {
                    ChestId = chest.ChestId,
                    Coins = chest.Credits,
                    IsUpdate = true,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
