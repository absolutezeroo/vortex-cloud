using System;
using System.Collections.Immutable;
using System.Linq;
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
/// The client asking what a chest holds, right after being told to open it.
/// </summary>
/// <remarks>
/// The answer carries <c>isUpdate = false</c>, which is what actually opens the screen on the
/// client's side; a true there would leave it closed and showing nothing.
/// </remarks>
public class OpenWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<OpenWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    /// <summary>How many stored items go in one page. The client assembles pages itself and has no
    /// opinion on the size; this one keeps a full chest off a single oversized packet.</summary>
    private const int ItemsPerPage = 100;

    public async ValueTask HandleAsync(
        OpenWiredChestMessage message,
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
            .OpenWiredChestAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                ct
            )
            .ConfigureAwait(false);

        if (chest is null)
        {
            return;
        }

        if (chest.IsCoinChest)
        {
            await ctx.SendComposerAsync(
                    new WiredChestCoinsMessageComposer
                    {
                        ChestId = chest.ChestId,
                        Coins = chest.Credits,
                        IsUpdate = false,
                    },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        ImmutableArray<FurnitureItemSnapshot>? items = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .ListWiredChestItemsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                ct
            )
            .ConfigureAwait(false);

        if (items is null)
        {
            return;
        }

        await SendItemPagesAsync(ctx, chest.ChestId, items.Value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the chest's contents as pages the client can assemble.
    /// </summary>
    /// <remarks>
    /// The screen only opens once the last page arrives, so an empty chest still gets one empty
    /// page. Splitting is ours to choose — the client just counts fragments — and a page is capped
    /// so a well-stocked chest does not become one packet the size of the room.
    /// </remarks>
    private static async Task SendItemPagesAsync(
        MessageContext ctx,
        int chestId,
        ImmutableArray<FurnitureItemSnapshot> items,
        CancellationToken ct
    )
    {
        int pages = Math.Max(1, (items.Length + ItemsPerPage - 1) / ItemsPerPage);

        for (int page = 0; page < pages; page++)
        {
            await ctx.SendComposerAsync(
                    new WiredChestItemsMessageComposer
                    {
                        ChestId = chestId,
                        TotalFragments = pages,
                        FragmentNo = page,
                        Items = [.. items.Skip(page * ItemsPerPage).Take(ItemsPerPage)],
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
