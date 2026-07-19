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
