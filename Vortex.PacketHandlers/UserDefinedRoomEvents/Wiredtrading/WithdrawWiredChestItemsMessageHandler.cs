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

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// Taking items of one kind back out of a chest.
/// </summary>
/// <remarks>
/// The answer is a delta, not a fresh listing: the client already drew the chest and only wants to
/// know which rows to drop. Nothing is sent when nothing moved, which is also what the client does
/// with an empty delta.
/// </remarks>
public class WithdrawWiredChestItemsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WithdrawWiredChestItemsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WithdrawWiredChestItemsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        ImmutableArray<int> removed = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .WithdrawWiredChestItemsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.IsWallItem,
                message.TypeId,
                message.LegacyPosterId,
                message.Count,
                ct
            )
            .ConfigureAwait(false);

        if (removed.IsDefaultOrEmpty)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new WiredChestItemsUpdateMessageComposer
                {
                    ChestId = message.ChestId,
                    RemovedItemIds = removed,
                    AddedItems = ImmutableArray<FurnitureItemSnapshot>.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
