using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Wired chests: the furni that hold real value for a room's wiring to hand out.
/// </summary>
public sealed partial class RoomGrain
{
    /// <summary>The classnames that are a chest, and which half they store.</summary>
    private static bool IsCoinChestClass(string className) =>
        className.StartsWith("wf_storage_coins", StringComparison.Ordinal);

    private static bool IsChestClass(string className) =>
        className.StartsWith("wf_storage_", StringComparison.Ordinal);

    public async Task<WiredChestSnapshot?> OpenWiredChestAsync(
        ActionContext ctx,
        int chestId,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || !_state.ItemsById.TryGetValue(chestId, out IRoomItem? item)
            || !IsChestClass(item.Definition.Name)
        )
        {
            return null;
        }

        // Only whoever may decorate the room may look inside: a chest is stock, and its contents are
        // not public just because the furni is.
        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        if (level == RoomControllerType.None)
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredChestEntity? chest = await dbCtx
                .WiredChests.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == chestId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (chest is null)
            {
                chest = new WiredChestEntity
                {
                    FurnitureEntityId = chestId,
                    Credits = 0,
                    NotificationsEnabled = true,
                };

                dbCtx.WiredChests.Add(chest);

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            return new WiredChestSnapshot
            {
                ChestId = chestId,
                Credits = chest.Credits,
                IsCoinChest = IsCoinChestClass(item.Definition.Name),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to open wired chest {ChestId} in room {RoomId}.",
                chestId,
                RoomId
            );

            return null;
        }
    }
}
