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
    ISessionGateway sessionGateway,
    ICfhTicketService cfhTickets,
    ITargetedOfferAdminService targetedOfferAdmin,
    IQuestAdminService questAdmin,
    INavigatorAdminService navigatorAdmin,
    IStaffAdminService staffAdmin,
    IAccountMfaService accountMfa,
    IContentAdminService contentAdmin,
    IMysteryBoxAdminService mysteryBoxAdmin,
    IPrizePoolAdminService prizePoolAdmin,
    IFurnitureAdminService furnitureAdmin,
    ISongAdminService songAdmin,
    IFishingAdminService fishingAdmin,
    IHabbiconAdminService habbiconAdmin,
    IRewardTrackAdminService rewardTrackAdmin,
    IRewardTrackCatalog rewardTrackCatalog,
    GamedataDocumentStore gamedata,
    IDatabaseBackupService databaseBackups,
    IForensicsPurgeService forensicsPurge,
    IBenchmarkService benchmark,
    OperationRunner runner,
    IVortexMetrics metrics,
    IConsoleCommandDispatcher consoleCommands,
    ILogger<DashboardOperationsService> logger
)
{
    /// <summary>
    /// Name of the reserved, account-less player row seeded by the
    /// <c>SeedDashboardStaffActor</c> migration. Room-scoped moderation grain methods
    /// (<c>MuteUserAsync</c>/<c>KickUserAsync</c>) require a real <see cref="PlayerId"/> as the
    /// acting player and reject <see cref="ActionContext.System"/> — this stands in for "the
    /// dashboard operator" since a web session has no in-game player of its own.
    /// </summary>
    private const string StaffActorName = "__dashboard_staff__";

    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly ICfhTicketService _cfhTickets = cfhTickets;
    private readonly ITargetedOfferAdminService _targetedOfferAdmin = targetedOfferAdmin;
    private readonly IQuestAdminService _questAdmin = questAdmin;
    private readonly INavigatorAdminService _navigatorAdmin = navigatorAdmin;
    private readonly IStaffAdminService _staffAdmin = staffAdmin;
    private readonly IAccountMfaService _accountMfa = accountMfa;
    private readonly IContentAdminService _contentAdmin = contentAdmin;
    private readonly IMysteryBoxAdminService _mysteryBoxAdmin = mysteryBoxAdmin;
    private readonly IPrizePoolAdminService _prizePoolAdmin = prizePoolAdmin;
    private readonly IFurnitureAdminService _furnitureAdmin = furnitureAdmin;
    private readonly ISongAdminService _songAdmin = songAdmin;
    private readonly IFishingAdminService _fishingAdmin = fishingAdmin;
    private readonly IHabbiconAdminService _habbiconAdmin = habbiconAdmin;
    private readonly IRewardTrackAdminService _rewardTrackAdmin = rewardTrackAdmin;

    /// <summary>
    /// Read only, and only to carry a track's current status through an edit that does not set one.
    /// Content writes go through <see cref="_rewardTrackAdmin"/>, which reloads this afterwards.
    /// </summary>
    private readonly IRewardTrackCatalog _rewardTrackCatalog = rewardTrackCatalog;

    private readonly GamedataDocumentStore _gamedata = gamedata;
    private readonly IDatabaseBackupService _databaseBackups = databaseBackups;
    private readonly IForensicsPurgeService _forensicsPurge = forensicsPurge;
    private readonly IBenchmarkService _benchmark = benchmark;
    private readonly OperationRunner _runner = runner;

    // Kept for GetActiveRoomsAsync, which times a grain call. Auditing a write is the
    // runner's business; this is not a write.
    private readonly IVortexMetrics _metrics = metrics;
    private readonly IConsoleCommandDispatcher _consoleCommands = consoleCommands;
    private readonly ILogger<DashboardOperationsService> _logger = logger;
    private readonly SemaphoreSlim _staffActorLock = new(1, 1);
    private PlayerId? _staffActorPlayerId;

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

    private async Task<PlayerId> ResolveStaffActorPlayerIdAsync(CancellationToken ct)
    {
        if (_staffActorPlayerId is { } cached)
        {
            return cached;
        }

        await _staffActorLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (_staffActorPlayerId is { } cachedAfterLock)
            {
                return cachedAfterLock;
            }

            PlayerId? resolved = await _grainFactory
                .GetPlayerDirectoryGrain()
                .GetPlayerIdAsync(StaffActorName, ct)
                .ConfigureAwait(false);

            if (resolved is null)
            {
                throw new InvalidOperationException("dashboard_staff_actor_missing");
            }

            _staffActorPlayerId = resolved.Value;

            return resolved.Value;
        }
        finally
        {
            _staffActorLock.Release();
        }
    }
}
