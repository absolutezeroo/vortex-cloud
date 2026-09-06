using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Fishing.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Fishing content admin operations. Each routes through
/// <see cref="Vortex.Primitives.Fishing.IFishingAdminService" /> — never a direct DB write — which
/// commits and then reloads the live definitions, pushing them to everyone currently fishing.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateFishingZoneAsync(
        CreateFishingZoneRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.zone.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.NameKey, request.FurniClass },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .CreateZoneAsync(
                            new FishingZoneSpec(
                                request.NameKey,
                                request.FurniClass,
                                request.RequiredLevel,
                                request.MinCatches,
                                request.MaxCatches
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdateFishingZoneAsync(
        UpdateFishingZoneRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.zone.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.ZoneId,
                request.NameKey,
                request.FurniClass,
            },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .UpdateZoneAsync(
                            request.ZoneId,
                            new FishingZoneSpec(
                                request.NameKey,
                                request.FurniClass,
                                request.RequiredLevel,
                                request.MinCatches,
                                request.MaxCatches
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteFishingZoneAsync(
        DeleteFishingZoneRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.zone.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.ZoneId },
            work: async c =>
                Throw(await _fishingAdmin.DeleteZoneAsync(request.ZoneId, c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> CreateFishingSpeciesAsync(
        CreateFishingSpeciesRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.species.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.ZoneId,
                request.NameKey,
                request.CatchRate,
                request.RarityWeight,
            },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .CreateSpeciesAsync(SpeciesSpec(request), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdateFishingSpeciesAsync(
        UpdateFishingSpeciesRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.species.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.SpeciesId,
                request.ZoneId,
                request.NameKey,
                request.CatchRate,
            },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .UpdateSpeciesAsync(request.SpeciesId, SpeciesSpec(request), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteFishingSpeciesAsync(
        DeleteFishingSpeciesRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.species.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.SpeciesId },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .DeleteSpeciesAsync(request.SpeciesId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> CreateFishingRodTierAsync(
        CreateFishingRodTierRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.rod.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Quality, request.NameKey },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .CreateRodTierAsync(RodSpec(request), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdateFishingRodTierAsync(
        UpdateFishingRodTierRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.rod.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.TierId, request.Quality },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .UpdateRodTierAsync(request.TierId, RodSpec(request), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteFishingRodTierAsync(
        DeleteFishingRodTierRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.rod.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.TierId },
            work: async c =>
                Throw(
                    await _fishingAdmin.DeleteRodTierAsync(request.TierId, c).ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> CreateFishingLevelAsync(
        CreateFishingLevelRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.level.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Level, request.XpThreshold },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .CreateLevelAsync(
                            new FishingLevelSpec(request.Level, request.XpThreshold),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdateFishingLevelAsync(
        UpdateFishingLevelRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.level.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.LevelId, request.Level },
            work: async c =>
                Throw(
                    await _fishingAdmin
                        .UpdateLevelAsync(
                            request.LevelId,
                            new FishingLevelSpec(request.Level, request.XpThreshold),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteFishingLevelAsync(
        DeleteFishingLevelRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.level.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.LevelId },
            work: async c =>
                Throw(
                    await _fishingAdmin.DeleteLevelAsync(request.LevelId, c).ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> ReloadFishingAsync(
        ReloadFishingRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.fishing.reload",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { },
            work: async c => Throw(await _fishingAdmin.ReloadAsync(c).ConfigureAwait(false)),
            ct
        );

    private static FishingSpeciesSpec SpeciesSpec(CreateFishingSpeciesRequest request) =>
        new(
            request.ZoneId,
            request.NameKey,
            request.RequiredLevel,
            request.RarityStars,
            request.CatchRate,
            request.RarityWeight,
            request.MinWeight,
            request.MaxWeight,
            request.XpReward,
            request.GoldenXpBonus,
            request.CurrencyReward,
            request.ActiveHours,
            request.ActiveWeekdays,
            request.ActiveSeasons
        );

    private static FishingSpeciesSpec SpeciesSpec(UpdateFishingSpeciesRequest request) =>
        new(
            request.ZoneId,
            request.NameKey,
            request.RequiredLevel,
            request.RarityStars,
            request.CatchRate,
            request.RarityWeight,
            request.MinWeight,
            request.MaxWeight,
            request.XpReward,
            request.GoldenXpBonus,
            request.CurrencyReward,
            request.ActiveHours,
            request.ActiveWeekdays,
            request.ActiveSeasons
        );

    private static FishingRodTierSpec RodSpec(CreateFishingRodTierRequest request) =>
        new(
            request.Quality,
            request.XpThreshold,
            request.NameKey,
            request.HandItemId,
            request.CatchMultiplier,
            request.GoldenMultiplier,
            request.HookHavocChance
        );

    private static FishingRodTierSpec RodSpec(UpdateFishingRodTierRequest request) =>
        new(
            request.Quality,
            request.XpThreshold,
            request.NameKey,
            request.HandItemId,
            request.CatchMultiplier,
            request.GoldenMultiplier,
            request.HookHavocChance
        );

    private static void Throw(FishingAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
