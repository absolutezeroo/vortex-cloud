using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    /// <summary>No configured cap exists for any wired resource in this codebase — reported as
    /// "no enforced limit" rather than a fabricated number.</summary>
    private const int NoEnforcedCap = int.MaxValue;

    public async Task<WiredRoomStatsEventMessageComposer> GetWiredRoomStatsAsync(
        CancellationToken ct
    )
    {
        ImmutableArray<RoomFloorItemSnapshot> floorItems = await GetAllFloorItemSnapshotsAsync(ct);
        ImmutableArray<RoomWallItemSnapshot> wallItems = await GetAllWallItemSnapshotsAsync(ct);

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        int[] floorItemIds = floorItems.Select(i => i.ObjectId.Value).ToArray();

        int permanentFurniVariables = await dbCtx
            .WiredPermanentVariables.AsNoTracking()
            .CountAsync(
                v =>
                    v.TargetType == WiredVariableTargetType.Furni
                    && floorItemIds.Contains(v.TargetId),
                ct
            );

        // Not room-scoped by design (Global/User variables are DB-global, not tied to any room),
        // reported as system-wide totals — see the wired-domain completion plan's residual-risk
        // notes.
        int permanentUserVariables = await dbCtx
            .WiredPermanentVariables.AsNoTracking()
            .CountAsync(v => v.TargetType == WiredVariableTargetType.User, ct);

        int permanentGlobalVariables = await dbCtx
            .WiredPermanentVariables.AsNoTracking()
            .CountAsync(v => v.TargetType == WiredVariableTargetType.Global, ct);

        return new WiredRoomStatsEventMessageComposer
        {
            // No wired execution-cost budget/tracking system exists in this codebase — reported
            // truthfully as zero/not-heavy rather than fabricated.
            ExecutionCost = 0,
            ExecutionCostCap = 0,
            IsHeavy = false,
            FloorItemCount = floorItems.Length,
            FloorItemCap = NoEnforcedCap,
            WallItemCount = wallItems.Length,
            WallItemCap = NoEnforcedCap,
            PermanentFurniVariables = permanentFurniVariables,
            MaxPermanentFurniVariables = NoEnforcedCap,
            PermanentUserVariables = permanentUserVariables,
            MaxPermanentUserVariables = NoEnforcedCap,
            PermanentGlobalVariables = permanentGlobalVariables,
            MaxPermanentGlobalVariables = NoEnforcedCap,
        };
    }
}
