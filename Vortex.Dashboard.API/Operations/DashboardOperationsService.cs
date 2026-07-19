using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Action;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

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
    IFurnitureAdminService furnitureAdmin,
    IAuditSink auditSink,
    IVortexContextAccessor context,
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
    private readonly IFurnitureAdminService _furnitureAdmin = furnitureAdmin;
    private readonly IAuditSink _auditSink = auditSink;
    private readonly IVortexContextAccessor _context = context;
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
        CorrelationId correlationId = CorrelationId.New();

        using IVortexTraceScope scope = _context.BeginScope(
            action,
            correlationId: correlationId,
            playerId: targetPlayerId,
            roomId: roomId
        );

        try
        {
            await work(ct).ConfigureAwait(false);

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
                category
            );

            return OperationResult.Succeeded(correlationId.Value);
        }
        catch (InvalidOperationException ex)
        {
            // Expected domain-validation rejection (e.g. duplicate voucher code) rather than an
            // infrastructure fault — logged at a lower severity and the reason is surfaced to the
            // operator instead of the generic "operation_failed".
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
                category
            );

            return OperationResult.Failed(correlationId.Value, ex.Message);
        }
        catch (Exception ex)
        {
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
                category
            );

            return OperationResult.Failed(correlationId.Value);
        }
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
        AuditCategory category = AuditCategory.Staff
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
                    }
                ),
            }
        );
}
