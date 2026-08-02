using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Prizes.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Prize pool admin operations. Each routes through
/// <see cref="Vortex.Primitives.Prizes.IPrizePoolAdminService"/> (never a direct DB write), which
/// reloads the live pool cache after committing, and emits a durable audit event with the operator's
/// reason — same contract as the catalog/quest operations.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreatePrizePoolAsync(
        CreatePrizePoolRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Code,
                request.Name,
                request.Variants,
            },
            work: async c =>
                Throw(
                    await _prizePoolAdmin
                        .CreatePoolAsync(
                            new PrizePoolSpec(
                                request.Code,
                                request.Name,
                                request.Variants,
                                request.Notes,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdatePrizePoolAsync(
        UpdatePrizePoolRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.PoolId,
                request.Code,
                request.Enabled,
            },
            work: async c =>
                Throw(
                    await _prizePoolAdmin
                        .UpdatePoolAsync(
                            request.PoolId,
                            new PrizePoolSpec(
                                request.Code,
                                request.Name,
                                request.Variants,
                                request.Notes,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeletePrizePoolAsync(
        DeletePrizePoolRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.PoolId },
            work: async c =>
                Throw(
                    await _prizePoolAdmin.DeletePoolAsync(request.PoolId, c).ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> CreatePrizeEntryAsync(
        CreatePrizeEntryRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.entry.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.PoolCode,
                request.Variant,
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
                        .CreateEntryAsync(
                            new PrizeEntrySpec(
                                request.PoolCode,
                                request.Variant,
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

    public Task<OperationResult> UpdatePrizeEntryAsync(
        UpdatePrizeEntryRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.entry.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.EntryId,
                request.PoolCode,
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
                            request.EntryId,
                            new PrizeEntrySpec(
                                request.PoolCode,
                                request.Variant,
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

    public Task<OperationResult> DeletePrizeEntryAsync(
        DeletePrizeEntryRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.entry.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.EntryId },
            work: async c =>
                Throw(
                    await _prizePoolAdmin.DeleteEntryAsync(request.EntryId, c).ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> ReloadPrizePoolsAsync(
        ReloadPrizePoolsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.reload",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { },
            work: async c => Throw(await _prizePoolAdmin.ReloadCacheAsync(c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> CreatePrizeBindingAsync(
        CreatePrizeBindingRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.binding.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.FurnitureDefinitionId,
                request.PoolCode,
                request.HitsRequired,
            },
            work: async c =>
                Throw(
                    await _prizePoolAdmin
                        .CreateBindingAsync(
                            new PrizeBindingSpec(
                                request.FurnitureDefinitionId,
                                request.PoolCode,
                                request.HitsRequired,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdatePrizeBindingAsync(
        UpdatePrizeBindingRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.binding.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.BindingId,
                request.PoolCode,
                request.HitsRequired,
            },
            work: async c =>
                Throw(
                    await _prizePoolAdmin
                        .UpdateBindingAsync(
                            request.BindingId,
                            new PrizeBindingSpec(
                                request.FurnitureDefinitionId,
                                request.PoolCode,
                                request.HitsRequired,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeletePrizeBindingAsync(
        DeletePrizeBindingRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.prizepool.binding.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.BindingId },
            work: async c =>
                Throw(
                    await _prizePoolAdmin
                        .DeleteBindingAsync(request.BindingId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );
}
