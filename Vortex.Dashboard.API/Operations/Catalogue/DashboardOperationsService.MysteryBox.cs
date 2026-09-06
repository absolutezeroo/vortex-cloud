using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox.Admin;
using Vortex.Primitives.Prizes.Admin;

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
                if (!TryParseProductType(request.ProductType, out ProductType productType))
                {
                    throw new InvalidOperationException("invalid_request");
                }

                Throw(
                    await _prizePoolAdmin
                        .CreateEntryAsync(
                            new PrizeEntrySpec(
                                request.Pool,
                                request.Color,
                                productType,
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
                if (!TryParseProductType(request.ProductType, out ProductType productType))
                {
                    throw new InvalidOperationException("invalid_request");
                }

                Throw(
                    await _prizePoolAdmin
                        .UpdateEntryAsync(
                            request.PrizeId,
                            new PrizeEntrySpec(
                                request.Pool,
                                request.Color,
                                productType,
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
                    await _prizePoolAdmin.DeleteEntryAsync(request.PrizeId, c).ConfigureAwait(false)
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
            {
                // Two caches back this page since the prizes moved to a shared pool: the box
                // definitions and the pool entries. Reloading one and not the other would leave the
                // operator's "reload" button half working, which is worse than not having it.
                Throw(await _mysteryBoxAdmin.ReloadCacheAsync(c).ConfigureAwait(false));
                Throw(await _prizePoolAdmin.ReloadCacheAsync(c).ConfigureAwait(false));
            },
            ct
        );

    /// <summary>The product type arrives as a string from the browser; a value outside the enum must
    /// fail the request rather than default to Floor and quietly grant the wrong kind of prize. The
    /// pool travels as its code and is resolved (and rejected when unknown) by the admin service.</summary>
    private static bool TryParseProductType(string productType, out ProductType parsed) =>
        Enum.TryParse(productType, ignoreCase: true, out parsed);

    private static void Throw(MysteryBoxAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorCode);
        }
    }

    private static void Throw(PrizeAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorCode);
        }
    }
}
