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
using Vortex.Primitives.Furniture.Admin;
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
    public Task<OperationResult> CreateFurnitureDefinitionAsync(
        CreateFurnitureDefinitionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.furniture.definition.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.SpriteId,
                request.Name,
                request.ProductType,
                request.FurniCategory,
            },
            work: async c =>
            {
                FurnitureAdminResult result = await _furnitureAdmin
                    .CreateAsync(
                        new FurnitureDefinitionUpsertSpec(
                            request.SpriteId,
                            request.Name,
                            request.ProductType,
                            request.FurniCategory,
                            request.Logic,
                            request.TotalStates,
                            request.Width,
                            request.Length,
                            request.StackHeight,
                            request.CanStack,
                            request.CanWalk,
                            request.CanSit,
                            request.CanLay,
                            request.CanRecycle,
                            request.CanTrade,
                            request.CanGroup,
                            request.CanSell,
                            request.UsagePolicy,
                            request.ExtraData,
                            request.StuffDataType
                        ),
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

    public Task<OperationResult> UpdateFurnitureDefinitionAsync(
        UpdateFurnitureDefinitionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.furniture.definition.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.DefinitionId,
                request.SpriteId,
                request.Name,
                request.ProductType,
                request.FurniCategory,
            },
            work: async c =>
            {
                FurnitureAdminResult result = await _furnitureAdmin
                    .UpdateAsync(
                        request.DefinitionId,
                        new FurnitureDefinitionUpsertSpec(
                            request.SpriteId,
                            request.Name,
                            request.ProductType,
                            request.FurniCategory,
                            request.Logic,
                            request.TotalStates,
                            request.Width,
                            request.Length,
                            request.StackHeight,
                            request.CanStack,
                            request.CanWalk,
                            request.CanSit,
                            request.CanLay,
                            request.CanRecycle,
                            request.CanTrade,
                            request.CanGroup,
                            request.CanSell,
                            request.UsagePolicy,
                            request.ExtraData,
                            request.StuffDataType
                        ),
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

    public Task<OperationResult> DeleteFurnitureDefinitionAsync(
        DeleteFurnitureDefinitionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.furniture.definition.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.DefinitionId },
            work: async c =>
            {
                FurnitureAdminResult result = await _furnitureAdmin
                    .DeleteAsync(request.DefinitionId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );
}
