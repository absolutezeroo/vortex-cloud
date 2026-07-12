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
using Turbo.Primitives.Furniture.Admin;
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
