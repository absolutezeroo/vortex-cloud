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
    ICatalogAdminService catalogAdmin,
    ITargetedOfferAdminService targetedOfferAdmin,
    IQuestAdminService questAdmin,
    IPollAdminService pollAdmin,
    IQuestContentAdminService questContentAdmin,
    INavigatorAdminService navigatorAdmin,
    IStaffAdminService staffAdmin,
    IAccountMfaService accountMfa,
    IContentAdminService contentAdmin,
    IWebArticleAdminService webArticleAdmin,
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
    IAuditSink auditSink,
    IVortexContextAccessor context,
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
    private readonly ICatalogAdminService _catalogAdmin = catalogAdmin;
    private readonly ITargetedOfferAdminService _targetedOfferAdmin = targetedOfferAdmin;
    private readonly IQuestAdminService _questAdmin = questAdmin;
    private readonly IPollAdminService _pollAdmin = pollAdmin;
    private readonly IQuestContentAdminService _questContentAdmin = questContentAdmin;
    private readonly INavigatorAdminService _navigatorAdmin = navigatorAdmin;
    private readonly IStaffAdminService _staffAdmin = staffAdmin;
    private readonly IAccountMfaService _accountMfa = accountMfa;
    private readonly IContentAdminService _contentAdmin = contentAdmin;
    private readonly IWebArticleAdminService _webArticleAdmin = webArticleAdmin;
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
    private readonly IAuditSink _auditSink = auditSink;
    private readonly IVortexContextAccessor _context = context;
    private readonly IVortexMetrics _metrics = metrics;
    private readonly IConsoleCommandDispatcher _consoleCommands = consoleCommands;
    private readonly ILogger<DashboardOperationsService> _logger = logger;
    private readonly SemaphoreSlim _staffActorLock = new(1, 1);
    private PlayerId? _staffActorPlayerId;

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

    /// <summary>
    /// Cross-cutting envelope for every operation: fresh correlation id + propagated trace scope,
    /// the grain/domain call, and a durable audit record on both success and failure. Failures are
    /// logged and returned as a non-throwing result so the operator sees the outcome and the id.
    /// </summary>
    private async Task<OperationResult> ExecuteAsync(
        string action,
        string actor,
        string reason,
        long? targetPlayerId,
        int? roomId,
        object detail,
        Func<CancellationToken, Task> work,
        CancellationToken ct,
        AuditCategory category = AuditCategory.Staff
    )
    {
        // The request's id when there is one, so the operation's audit row, the HTTP access row and
        // the error the operator is holding all name the same thing. A fresh id here meant three ids
        // for one failed click.
        CorrelationId correlationId = _context.Current?.CorrelationId ?? CorrelationId.New();

        using IVortexTraceScope scope = _context.BeginScope(
            action,
            correlationId: correlationId,
            playerId: targetPlayerId,
            roomId: roomId
        );

        // Armed for the duration of the domain call so the audit can say what the write replaced,
        // not merely which id it was pointed at. Before this, a delete recorded `{ offerId: 12 }`
        // and the row itself was gone -- there was nowhere left to read what it had been.
        using IEntityChangeCapture capture = EntityChangeCapture.Begin();

        // One timestamp for all three exits. The audit row says what happened and the trail is
        // queryable, but a table is not an alert: an action that has started failing, or one that has
        // quietly gone from 40ms to four seconds, is invisible until somebody thinks to look.
        long startedAt = Stopwatch.GetTimestamp();

        try
        {
            await work(ct).ConfigureAwait(false);

            Measure(action, "success", startedAt);

            Emit(
                action,
                AuditResult.Success,
                AuditSeverity.Notice,
                correlationId,
                actor,
                reason,
                targetPlayerId,
                roomId,
                detail,
                category,
                capture.Changes
            );

            return OperationResult.Succeeded(correlationId.Value);
        }
        catch (InvalidOperationException ex) when (IsDomainCode(ex.Message))
        {
            // Expected domain-validation rejection (e.g. duplicate voucher code) rather than an
            // infrastructure fault — logged at a lower severity and the reason is surfaced to the
            // operator instead of the generic "operation_failed".
            //
            // Guarded by the shape of the message, because InvalidOperationException is not only the
            // domain's: EF throws it too ("Sequence contains no elements", "The instance of entity
            // type cannot be tracked..."), and this branch put whatever it said on the operator's
            // screen. One filtered `when` sends those to the fault branch below instead, which logs
            // the exception and answers a generic code -- no schema, no query, no stack.
            Measure(action, "rejected", startedAt);

            _logger.LogInformation(
                VortexEventIds.DashboardFault,
                "Dashboard operation {Action} rejected: {Reason}",
                action,
                ex.Message
            );

            Emit(
                action,
                AuditResult.Failed,
                AuditSeverity.Notice,
                correlationId,
                actor,
                reason,
                targetPlayerId,
                roomId,
                detail,
                category,
                // Usually empty -- a domain rejection happens before anything is written. When it is
                // not, a row was saved before the refusal, and that is exactly the case worth seeing.
                capture.Changes
            );

            return OperationResult.Failed(correlationId.Value, ex.Message);
        }
        catch (Exception ex)
        {
            Measure(action, "failed", startedAt);

            _logger.LogError(
                VortexEventIds.DashboardFault,
                ex,
                "Dashboard operation {Action} failed",
                action
            );

            Emit(
                action,
                AuditResult.Failed,
                AuditSeverity.Warning,
                correlationId,
                actor,
                reason,
                targetPlayerId,
                roomId,
                detail,
                category,
                capture.Changes
            );

            return OperationResult.Failed(correlationId.Value);
        }
    }

    /// <summary>
    ///     Whether an exception message is one of the domain's own rejection codes rather than a
    ///     framework sentence. Every deliberate one is lowercase snake_case — <c>offer_has_products</c>,
    ///     <c>account_not_found</c> — and nothing thrown by EF, Orleans or the BCL looks like that.
    /// </summary>
    internal static bool IsDomainCode(string? message) =>
        !string.IsNullOrEmpty(message)
        && message.Length <= 64
        && DomainCodePattern().IsMatch(message);

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex DomainCodePattern();

    private void Measure(string action, string outcome, long startedAt) =>
        _metrics.DashboardOperationCompleted(
            action,
            outcome,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds
        );

    /// <summary>
    ///     Whether the name an operation was given differs from the operator the request arrived as.
    /// </summary>
    /// <remarks>
    ///     Null outside a request — a console command or a background sweep has no session to compare
    ///     against, and "no opinion" is the honest value there rather than false. Null too when they
    ///     agree, so the field only appears in the rows worth looking at.
    /// </remarks>
    private static bool? Mismatched(string actor)
    {
        ActorSecurityContext? current = ActorSecurityContext.Current;

        if (current is null)
        {
            return null;
        }

        return string.Equals(actor, current.Email, StringComparison.OrdinalIgnoreCase)
            ? null
            : true;
    }

    private void Emit(
        string action,
        AuditResult result,
        AuditSeverity severity,
        CorrelationId correlationId,
        string actor,
        string reason,
        long? targetPlayerId,
        int? roomId,
        object detail,
        AuditCategory category = AuditCategory.Staff,
        IReadOnlyList<EntityChange>? changes = null
    ) =>
        _auditSink.Emit(
            new AuditEvent
            {
                Category = category,
                Action = action,
                Severity = severity,
                Result = result,
                CorrelationId = correlationId,
                TargetPlayerId = targetPlayerId,
                RoomId = roomId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        actor,
                        reason,
                        detail,
                        // The server's own view of who asked, next to the name the caller passed.
                        // `actor` is an argument -- every operation forwards one and nothing checks
                        // it -- so an audit trail built on it alone records what it was told. This
                        // records the account behind the session the request actually arrived on,
                        // and `actorMismatch` is the case worth finding: a row where the two
                        // disagree is either a bug in a call site or somebody writing under a name
                        // that is not theirs.
                        actorAccountId = ActorSecurityContext.Current?.AccountId,
                        actorMismatch = Mismatched(actor),
                        // Omitted rather than written as an empty array: most operations touch no
                        // tracked row (they call a grain, or use a bulk statement), and a `changes: []`
                        // on every one of those would read as "nothing changed" instead of "not
                        // recorded here".
                        changes = changes is { Count: > 0 } ? changes : null,
                    }
                ),
            }
        );
}
