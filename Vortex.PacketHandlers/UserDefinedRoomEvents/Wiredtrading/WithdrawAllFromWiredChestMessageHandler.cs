using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Emptying a chest into whoever asked.
/// </summary>
/// <remarks>
/// Zero means "everything" to the room. The reply carries <c>isUpdate = true</c>: the screen is already open, and a false there would
/// make the client treat this as a fresh opening.
/// </remarks>
public class WithdrawAllFromWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WithdrawAllFromWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WithdrawAllFromWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredChestSnapshot? chest = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .WithdrawWiredChestCreditsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                0,
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
