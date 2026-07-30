using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Mystery box admin operations. Each routes through
/// <see cref="Vortex.Primitives.MysteryBox.IMysteryBoxAdminService"/> (never a direct DB write),
/// which reloads the live definition/prize cache after committing, and emits a durable audit event
/// with the operator's reason — same contract as the catalog/quest operations.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateMysteryBoxPrizeAsync(
        CreateMysteryBoxPrizeRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.mysterybox.prize.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Pool,
                request.Color,
                request.ProductType,
                request.FurnitureDefinitionId,
                request.Weight,
            },
            work: async c =>
            {
                if (!TryParsePrizeSpec(request.Pool, request.ProductType, out var parsed))
                {
                    throw new InvalidOperationException("invalid_request");
                }

                Throw(
                    await _mysteryBoxAdmin
                        .CreatePrizeAsync(
                            new MysteryBoxPrizeSpec(
                                parsed.Pool,
                                request.Color,
                                parsed.ProductType,
                                request.FurnitureDefinitionId,
                                request.ExtraParam,
                                request.Weight,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> UpdateMysteryBoxPrizeAsync(
        UpdateMysteryBoxPrizeRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.mysterybox.prize.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.PrizeId,
                request.Pool,
                request.Color,
                request.ProductType,
                request.Weight,
            },
            work: async c =>
            {
                if (!TryParsePrizeSpec(request.Pool, request.ProductType, out var parsed))
                {
                    throw new InvalidOperationException("invalid_request");
                }

                Throw(
                    await _mysteryBoxAdmin
                        .UpdatePrizeAsync(
                            request.PrizeId,
                            new MysteryBoxPrizeSpec(
                                parsed.Pool,
                                request.Color,
                                parsed.ProductType,
                                request.FurnitureDefinitionId,
                                request.ExtraParam,
                                request.Weight,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> DeleteMysteryBoxPrizeAsync(
        DeleteMysteryBoxPrizeRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.mysterybox.prize.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.PrizeId },
            work: async c =>
                Throw(
                    await _mysteryBoxAdmin
                        .DeletePrizeAsync(request.PrizeId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> GrantMysteryBoxKeyAsync(
        GrantMysteryBoxKeyRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.mysterybox.key.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.Color },
            work: async c =>
                Throw(
                    await _mysteryBoxAdmin
                        .GrantKeyAsync(request.PlayerId, request.Color, actor, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> GrantMysteryBoxAsync(
        GrantMysteryBoxRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.mysterybox.box.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.FurnitureDefinitionId, request.Color },
            work: async c =>
                Throw(
                    await _mysteryBoxAdmin
                        .GrantBoxAsync(
                            request.PlayerId,
                            request.FurnitureDefinitionId,
                            request.Color,
                            actor,
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> ReloadMysteryBoxAsync(
        ReloadMysteryBoxRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.mysterybox.reload",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { },
            work: async c =>
                Throw(await _mysteryBoxAdmin.ReloadCacheAsync(c).ConfigureAwait(false)),
            ct
        );

    /// <summary>The pool and product type arrive as strings from the browser; a value outside the
    /// enums must fail the request rather than default to Box/Floor and quietly file the prize in the
    /// wrong pool.</summary>
    private static bool TryParsePrizeSpec(
        string pool,
        string productType,
        out (MysteryBoxPrizePool Pool, ProductType ProductType) parsed
    )
    {
        parsed = default;

        if (!Enum.TryParse(pool, ignoreCase: true, out MysteryBoxPrizePool parsedPool))
        {
            return false;
        }

        if (!Enum.TryParse(productType, ignoreCase: true, out ProductType parsedProductType))
        {
            return false;
        }

        parsed = (parsedPool, parsedProductType);

        return true;
    }

    private static void Throw(MysteryBoxAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorCode);
        }
    }
}
