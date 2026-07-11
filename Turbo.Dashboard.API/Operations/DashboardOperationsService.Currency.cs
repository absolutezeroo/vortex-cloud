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
}
