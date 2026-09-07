using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Security;
using Vortex.Database.Auditing;
using Vortex.Database.Backup;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Action;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Benchmark;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Console;
using Vortex.Primitives.Content;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Prizes;
using Vortex.Primitives.Quests;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Primitives.Sound;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Executes controlled admin operations for the dashboard. This is deliberately separate from the
/// read-only <c>DashboardApiService</c>: every action here is routed through the existing
/// grains/domain services (never a direct DB write), carries a mandatory reason, runs under a fresh
/// correlation id, and emits a durable <see cref="AuditEvent"/> regardless of outcome.
/// </summary>
internal sealed partial class DashboardOperationsService(
    IGrainFactory grainFactory,
    StaffActorAccount staffActor,
    ISessionGateway sessionGateway,
    OperationRunner runner,
    IVortexMetrics metrics
)
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly StaffActorAccount _staffActor = staffActor;
    private readonly ISessionGateway _sessionGateway = sessionGateway;

    /// <summary>
    /// Read only, and only to carry a track's current status through an edit that does not set one.
    /// Content writes go through <see cref="_rewardTrackAdmin"/>, which reloads this afterwards.
    /// </summary>
    private readonly OperationRunner _runner = runner;

    // Kept for GetActiveRoomsAsync, which times a grain call. Auditing a write is the
    // runner's business; this is not a write.
    private readonly IVortexMetrics _metrics = metrics;

    /// <summary>
    /// Forwards to <see cref="OperationRunner"/>, so the twenty-six topic files that still live on
    /// this class keep calling the same thing while the mechanism itself has already moved. Each
    /// subject drops this line when it becomes its own class and takes the runner directly.
    /// </summary>
    private Task<OperationResult> ExecuteAsync(
        string action,
        string actor,
        string reason,
        long? targetPlayerId,
        int? roomId,
        object detail,
        Func<CancellationToken, Task> work,
        CancellationToken ct,
        AuditCategory category = AuditCategory.Staff
    ) =>
        _runner.ExecuteAsync(
            action,
            actor,
            reason,
            targetPlayerId,
            roomId,
            detail,
            work,
            ct,
            category
        );

    // The lookup lives in StaffActorAccount now; this stays while Rooms is still on this class.
    private Task<PlayerId> ResolveStaffActorPlayerIdAsync(CancellationToken ct) =>
        _staffActor.PlayerIdAsync(ct);
}
