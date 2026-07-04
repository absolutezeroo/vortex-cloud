using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums.Wallet;

namespace Turbo.Dashboard.API.Operations;

/// <summary>
/// Executes controlled admin operations for the dashboard. This is deliberately separate from the
/// read-only <c>DashboardApiService</c>: every action here is routed through the existing
/// grains/domain services (never a direct DB write), carries a mandatory reason, runs under a fresh
/// correlation id, and emits a durable <see cref="AuditEvent"/> regardless of outcome.
/// </summary>
internal sealed class DashboardOperationsService(
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    IAuditSink auditSink,
    ITurboContextAccessor context,
    ILogger<DashboardOperationsService> logger
)
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly IAuditSink _auditSink = auditSink;
    private readonly ITurboContextAccessor _context = context;
    private readonly ILogger<DashboardOperationsService> _logger = logger;

    public Task<OperationResult> GiveCreditsAsync(
        GiveCreditsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.currency.credits.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { currency = "credits", request.Amount },
            work: c =>
                _grainFactory
                    .GetPlayerWalletGrain(new PlayerId(request.PlayerId))
                    .GrantCreditsAsync(request.Amount, c),
            ct
        );

    public Task<OperationResult> GiveActivityPointsAsync(
        GiveActivityPointsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.currency.activitypoints.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new
            {
                currency = "activity_points",
                request.Type,
                request.Amount,
            },
            work: c =>
                _grainFactory
                    .GetPlayerWalletGrain(new PlayerId(request.PlayerId))
                    .GrantActivityPointsAsync(request.Type, request.Amount, c),
            ct
        );

    public Task<OperationResult> GiveFurnitureAsync(
        GiveFurnitureRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.item.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.DefinitionId, request.ExtraData },
            work: c =>
                _grainFactory
                    .GetInventoryGrain(new PlayerId(request.PlayerId))
                    .GrantFurnitureDefinitionAsync(request.DefinitionId, request.ExtraData, c),
            ct
        );

    public Task<OperationResult> CreateVoucherAsync(
        CreateVoucherRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.vouchers.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Code,
                request.CurrencyType,
                request.ActivityPointType,
                request.Amount,
                request.MaxRedemptions,
                request.ExpiresAt,
            },
            work: async c =>
            {
                VoucherCreateResult result = await _grainFactory
                    .GetVoucherGrain(request.Code)
                    .CreateAsync(
                        new VoucherCreateSpec
                        {
                            CurrencyType = (CurrencyType)request.CurrencyType,
                            ActivityPointType = request.ActivityPointType,
                            Amount = request.Amount,
                            MaxRedemptions = request.MaxRedemptions,
                            ExpiresAt = request.ExpiresAt,
                            CreatedBy = actor,
                        },
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeactivateVoucherAsync(
        DeactivateVoucherRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.vouchers.deactivate",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Code },
            work: async c =>
            {
                VoucherCreateResult result = await _grainFactory
                    .GetVoucherGrain(request.Code)
                    .DeactivateAsync(c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<VoucherSnapshot> GetVoucherSnapshotAsync(string code, CancellationToken ct) =>
        _grainFactory.GetVoucherGrain(code).GetSnapshotAsync(ct);

    public Task<OperationResult> KickPlayerAsync(
        KickPlayerRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.player.kick",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { },
            work: c =>
                _sessionGateway.RemoveSessionFromPlayerAsync(new PlayerId(request.PlayerId), c),
            ct
        );

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
        CancellationToken ct
    )
    {
        CorrelationId correlationId = CorrelationId.New();

        using ITurboTraceScope scope = _context.BeginScope(
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
                detail
            );

            return OperationResult.Succeeded(correlationId.Value);
        }
        catch (InvalidOperationException ex)
        {
            // Expected domain-validation rejection (e.g. duplicate voucher code) rather than an
            // infrastructure fault — logged at a lower severity and the reason is surfaced to the
            // operator instead of the generic "operation_failed".
            _logger.LogInformation(
                TurboEventIds.DashboardFault,
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
                detail
            );

            return OperationResult.Failed(correlationId.Value, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                TurboEventIds.DashboardFault,
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
                detail
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
        object detail
    ) =>
        _auditSink.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Staff,
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
