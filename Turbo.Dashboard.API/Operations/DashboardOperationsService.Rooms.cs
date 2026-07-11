using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Action;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Dashboard.API.Operations;

internal sealed partial class DashboardOperationsService
{
    public Task<ImmutableArray<RoomSummarySnapshot>> GetActiveRoomsAsync() =>
        _grainFactory.GetRoomDirectoryGrain().GetActiveRoomsAsync();

    public async Task<ImmutableArray<RoomOccupantSnapshot>> GetRoomOccupantsAsync(
        int roomId,
        CancellationToken ct
    )
    {
        ImmutableArray<RoomAvatarSnapshot> avatars = await _grainFactory
            .GetRoomGrain(new RoomId(roomId))
            .GetAllAvatarSnapshotsAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. avatars
                .OfType<RoomPlayerAvatarSnapshot>()
                .Select(a => new RoomOccupantSnapshot { PlayerId = a.WebId, Name = a.Name }),
        ];
    }

    public Task<OperationResult> ForceCloseRoomAsync(
        ForceCloseRoomRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.room.close",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: request.RoomId,
            detail: new { },
            work: _ => _grainFactory.GetRoomGrain(new RoomId(request.RoomId)).DeactivateRoomAsync(),
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> KickFromRoomAsync(
        KickFromRoomRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.room.kick",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: request.RoomId,
            detail: new { },
            work: async c =>
            {
                PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c).ConfigureAwait(false);
                RoomId roomId = new(request.RoomId);
                ActionContext actorCtx = ActionContext.CreateForPlayer(staffActor, roomId);

                bool ok = await _grainFactory
                    .GetRoomGrain(roomId)
                    .KickUserAsync(actorCtx, new PlayerId(request.PlayerId), c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("kick_rejected");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    /// <summary>
    /// Resolves (and caches for the process lifetime) the reserved dashboard staff actor's player
    /// id by name via the player directory — see <see cref="StaffActorName"/>. Throws if the
    /// <c>SeedDashboardStaffActor</c> migration has not run.
    /// </summary>
}
