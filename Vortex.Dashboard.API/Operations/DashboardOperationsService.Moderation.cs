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
using Vortex.Primitives.Catalog.Snapshots;
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

internal sealed partial class DashboardOperationsService
{
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

    public Task<OperationResult> BanPlayerAsync(
        BanPlayerRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.player.ban",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.Permanent, request.DurationSeconds },
            work: async c =>
            {
                PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c).ConfigureAwait(false);
                DateTime bannedUntil = request.Permanent
                    ? SanctionDuration.Permanent
                    : DateTime.UtcNow.AddSeconds(Math.Max(1, request.DurationSeconds ?? 0));

                bool ok = await _grainFactory
                    .GetPlayerGrain(new PlayerId(request.PlayerId))
                    .ApplyAccountBanAsync(staffActor, bannedUntil, request.Reason, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("no_linked_account");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> UnbanPlayerAsync(
        UnbanPlayerRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.player.unban",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { },
            work: async c =>
            {
                PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c).ConfigureAwait(false);

                bool ok = await _grainFactory
                    .GetPlayerGrain(new PlayerId(request.PlayerId))
                    .ApplyAccountBanAsync(staffActor, bannedUntil: null, request.Reason, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("no_linked_account");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> TradingLockAsync(
        TradingLockRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.player.trading_lock",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.Permanent, request.DurationSeconds },
            work: async c =>
            {
                PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c).ConfigureAwait(false);
                DateTime lockedUntil = request.Permanent
                    ? SanctionDuration.Permanent
                    : DateTime.UtcNow.AddSeconds(Math.Max(1, request.DurationSeconds ?? 0));

                bool ok = await _grainFactory
                    .GetPlayerGrain(new PlayerId(request.PlayerId))
                    .ApplyTradingLockAsync(staffActor, lockedUntil, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("no_linked_account");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> TradingUnlockAsync(
        TradingUnlockRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.player.trading_unlock",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { },
            work: async c =>
            {
                PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c).ConfigureAwait(false);

                bool ok = await _grainFactory
                    .GetPlayerGrain(new PlayerId(request.PlayerId))
                    .ApplyTradingLockAsync(staffActor, lockedUntil: null, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("no_linked_account");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public async Task<OperationResult> MutePlayerAsync(
        MutePlayerRequest request,
        string actor,
        CancellationToken ct
    )
    {
        PlayerId target = new(request.PlayerId);
        RoomPointerSnapshot activeRoom = await _grainFactory
            .GetPlayerPresenceGrain(target)
            .GetActiveRoomAsync()
            .ConfigureAwait(false);

        return await ExecuteAsync(
                "ops.player.mute",
                actor,
                request.Reason,
                targetPlayerId: request.PlayerId,
                roomId: activeRoom.RoomId > 0 ? activeRoom.RoomId.Value : null,
                detail: new { request.DurationSeconds },
                work: async c =>
                {
                    // Room-scoped mute is the only mute primitive in this codebase — there is no
                    // account-wide chat mute. This rejects up front (before the grain call) rather
                    // than letting MuteUserAsync silently no-op for a roomless target.
                    if (activeRoom.RoomId <= 0)
                    {
                        throw new InvalidOperationException("target_not_in_room");
                    }

                    PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c)
                        .ConfigureAwait(false);
                    ActionContext actorCtx = ActionContext.CreateForPlayer(
                        staffActor,
                        activeRoom.RoomId
                    );

                    bool ok = await _grainFactory
                        .GetRoomGrain(activeRoom.RoomId)
                        .MuteUserAsync(actorCtx, target, request.DurationSeconds, c)
                        .ConfigureAwait(false);

                    if (!ok)
                    {
                        throw new InvalidOperationException("mute_rejected");
                    }
                },
                ct,
                AuditCategory.Moderation
            )
            .ConfigureAwait(false);
    }

    public Task<ImmutableArray<CfhIssueQueueEntrySnapshot>> GetCfhQueueAsync(
        CancellationToken ct
    ) => _cfhTickets.GetOpenQueueAsync(ct);

    public Task<OperationResult> PickCfhTicketsAsync(
        PickCfhTicketsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.cfh.pick",
            actor,
            "cfh pick",
            targetPlayerId: null,
            roomId: null,
            detail: new { request.IssueIds },
            work: async c =>
            {
                PlayerId staffActor = await ResolveStaffActorPlayerIdAsync(c).ConfigureAwait(false);

                await _cfhTickets
                    .PickTicketsAsync(request.IssueIds, staffActor.Value, c)
                    .ConfigureAwait(false);
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> CloseCfhTicketsAsync(
        CloseCfhTicketsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.cfh.close",
            actor,
            "cfh close",
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.IssueIds,
                request.Reason,
                request.Sanctioned,
            },
            work: c =>
                _cfhTickets.CloseTicketsAsync(
                    request.IssueIds,
                    (CfhTicketCloseReason)request.Reason,
                    request.Sanctioned,
                    c
                ),
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> ReleaseCfhTicketsAsync(
        ReleaseCfhTicketsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.cfh.release",
            actor,
            "cfh release",
            targetPlayerId: null,
            roomId: null,
            detail: new { request.IssueIds },
            work: c => _cfhTickets.ReleaseTicketsAsync(request.IssueIds, c),
            ct,
            AuditCategory.Moderation
        );
}
