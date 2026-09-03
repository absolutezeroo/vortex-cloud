using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Fishing;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Fishing.Admin;
using Vortex.Primitives.Orleans;

namespace Vortex.Fishing;

/// <summary>
/// Writes to the four fishing content tables, each followed by a live reload.
/// </summary>
/// <remarks>
/// The reload is the point of the whole design: <c>FishingDefinitionsGrain.ReloadAsync</c> rebuilds
/// the cache <em>and</em> pushes the new definitions to everyone currently fishing, so a retuned
/// catch rate reaches a player mid-session. A write that went straight to the database would leave
/// every connected player on the old numbers with nothing saying so.
/// </remarks>
internal sealed class FishingAdminService(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory
) : IFishingAdminService
{
    public Task<FishingAdminResult> CreateZoneAsync(FishingZoneSpec spec, CancellationToken ct) =>
        WriteAsync(
            spec,
            ValidateZone,
            async (dbCtx, c) =>
            {
                if (
                    await ZoneClassTakenAsync(dbCtx, spec.FurniClass, null, c).ConfigureAwait(false)
                )
                {
                    return null;
                }

                FishingZoneEntity entity = new()
                {
                    NameKey = spec.NameKey.Trim(),
                    FurniClass = spec.FurniClass.Trim(),
                    RequiredLevel = spec.RequiredLevel,
                    MinCatches = spec.MinCatches,
                    MaxCatches = spec.MaxCatches,
                };

                dbCtx.FishingZones.Add(entity);

                return entity;
            },
            "furni_class_taken",
            ct
        );

    public Task<FishingAdminResult> UpdateZoneAsync(
        int zoneId,
        FishingZoneSpec spec,
        CancellationToken ct
    ) =>
        WriteAsync(
            spec,
            ValidateZone,
            async (dbCtx, c) =>
            {
                FishingZoneEntity? entity = await dbCtx
                    .FishingZones.FirstOrDefaultAsync(z => z.Id == zoneId, c)
                    .ConfigureAwait(false);

                if (
                    entity is null
                    || await ZoneClassTakenAsync(dbCtx, spec.FurniClass, zoneId, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                entity.NameKey = spec.NameKey.Trim();
                entity.FurniClass = spec.FurniClass.Trim();
                entity.RequiredLevel = spec.RequiredLevel;
                entity.MinCatches = spec.MinCatches;
                entity.MaxCatches = spec.MaxCatches;

                return entity;
            },
            "zone_not_found_or_class_taken",
            ct
        );

    public Task<FishingAdminResult> DeleteZoneAsync(int zoneId, CancellationToken ct) =>
        WriteAsync<object?>(
            null,
            _ => null,
            async (dbCtx, c) =>
            {
                // A species whose zone is gone can never be drawn, and nothing at runtime would say
                // so — the zone lookup would simply never match. Refusing here is the only place
                // that can still explain why.
                if (
                    await dbCtx
                        .FishingSpecies.AnyAsync(s => s.ZoneId == zoneId, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                FishingZoneEntity? entity = await dbCtx
                    .FishingZones.FirstOrDefaultAsync(z => z.Id == zoneId, c)
                    .ConfigureAwait(false);

                if (entity is null)
                {
                    return null;
                }

                dbCtx.FishingZones.Remove(entity);

                return entity;
            },
            "zone_in_use_or_not_found",
            ct
        );

    public Task<FishingAdminResult> CreateSpeciesAsync(
        FishingSpeciesSpec spec,
        CancellationToken ct
    ) =>
        WriteAsync(
            spec,
            ValidateSpecies,
            async (dbCtx, c) =>
            {
                if (
                    !await dbCtx
                        .FishingZones.AnyAsync(z => z.Id == spec.ZoneId, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                FishingSpeciesEntity entity = new()
                {
                    ZoneId = spec.ZoneId,
                    NameKey = string.Empty,
                };

                Apply(entity, spec);
                dbCtx.FishingSpecies.Add(entity);

                return entity;
            },
            "zone_not_found",
            ct
        );

    public Task<FishingAdminResult> UpdateSpeciesAsync(
        int speciesId,
        FishingSpeciesSpec spec,
        CancellationToken ct
    ) =>
        WriteAsync(
            spec,
            ValidateSpecies,
            async (dbCtx, c) =>
            {
                FishingSpeciesEntity? entity = await dbCtx
                    .FishingSpecies.FirstOrDefaultAsync(s => s.Id == speciesId, c)
                    .ConfigureAwait(false);

                if (
                    entity is null
                    || !await dbCtx
                        .FishingZones.AnyAsync(z => z.Id == spec.ZoneId, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                Apply(entity, spec);

                return entity;
            },
            "species_or_zone_not_found",
            ct
        );

    public Task<FishingAdminResult> DeleteSpeciesAsync(int speciesId, CancellationToken ct) =>
        DeleteAsync<FishingSpeciesEntity>(speciesId, ct);

    public Task<FishingAdminResult> CreateRodTierAsync(
        FishingRodTierSpec spec,
        CancellationToken ct
    ) =>
        WriteAsync(
            spec,
            ValidateRodTier,
            async (dbCtx, c) =>
            {
                if (
                    await dbCtx
                        .FishingRodTiers.AnyAsync(t => t.Quality == spec.Quality, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                FishingRodTierEntity entity = new()
                {
                    Quality = spec.Quality,
                    NameKey = spec.NameKey.Trim(),
                };

                Apply(entity, spec);
                dbCtx.FishingRodTiers.Add(entity);

                return entity;
            },
            "quality_taken",
            ct
        );

    public Task<FishingAdminResult> UpdateRodTierAsync(
        int tierId,
        FishingRodTierSpec spec,
        CancellationToken ct
    ) =>
        WriteAsync(
            spec,
            ValidateRodTier,
            async (dbCtx, c) =>
            {
                FishingRodTierEntity? entity = await dbCtx
                    .FishingRodTiers.FirstOrDefaultAsync(t => t.Id == tierId, c)
                    .ConfigureAwait(false);

                if (
                    entity is null
                    || await dbCtx
                        .FishingRodTiers.AnyAsync(
                            t => t.Quality == spec.Quality && t.Id != tierId,
                            c
                        )
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                Apply(entity, spec);

                return entity;
            },
            "tier_not_found_or_quality_taken",
            ct
        );

    public Task<FishingAdminResult> DeleteRodTierAsync(int tierId, CancellationToken ct) =>
        DeleteAsync<FishingRodTierEntity>(tierId, ct);

    public Task<FishingAdminResult> CreateLevelAsync(FishingLevelSpec spec, CancellationToken ct) =>
        WriteAsync(
            spec,
            ValidateLevel,
            async (dbCtx, c) =>
            {
                if (
                    await dbCtx
                        .FishingLevels.AnyAsync(l => l.Level == spec.Level, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                FishingLevelEntity entity = new()
                {
                    Level = spec.Level,
                    XpThreshold = spec.XpThreshold,
                };

                dbCtx.FishingLevels.Add(entity);

                return entity;
            },
            "level_taken",
            ct
        );

    public Task<FishingAdminResult> UpdateLevelAsync(
        int levelId,
        FishingLevelSpec spec,
        CancellationToken ct
    ) =>
        WriteAsync(
            spec,
            ValidateLevel,
            async (dbCtx, c) =>
            {
                FishingLevelEntity? entity = await dbCtx
                    .FishingLevels.FirstOrDefaultAsync(l => l.Id == levelId, c)
                    .ConfigureAwait(false);

                if (
                    entity is null
                    || await dbCtx
                        .FishingLevels.AnyAsync(l => l.Level == spec.Level && l.Id != levelId, c)
                        .ConfigureAwait(false)
                )
                {
                    return null;
                }

                entity.Level = spec.Level;
                entity.XpThreshold = spec.XpThreshold;

                return entity;
            },
            "level_not_found_or_taken",
            ct
        );

    public Task<FishingAdminResult> DeleteLevelAsync(int levelId, CancellationToken ct) =>
        DeleteAsync<FishingLevelEntity>(levelId, ct);

    public async Task<FishingAdminResult> ReloadAsync(CancellationToken ct) =>
        FishingAdminResult.Ok(0, await ReloadDefinitionsAsync(ct).ConfigureAwait(false));

    private static void Apply(FishingSpeciesEntity entity, FishingSpeciesSpec spec)
    {
        entity.ZoneId = spec.ZoneId;
        entity.NameKey = spec.NameKey.Trim();
        entity.RequiredLevel = spec.RequiredLevel;
        entity.RarityStars = spec.RarityStars;
        entity.CatchRate = spec.CatchRate;
        entity.RarityWeight = spec.RarityWeight;
        entity.MinWeight = spec.MinWeight;
        entity.MaxWeight = spec.MaxWeight;
        entity.XpReward = spec.XpReward;
        entity.GoldenXpBonus = spec.GoldenXpBonus;
        entity.CurrencyReward = spec.CurrencyReward;
        entity.ActiveHours = spec.ActiveHours;
        entity.ActiveWeekdays = spec.ActiveWeekdays;
        entity.ActiveSeasons = spec.ActiveSeasons;
    }

    private static void Apply(FishingRodTierEntity entity, FishingRodTierSpec spec)
    {
        entity.Quality = spec.Quality;
        entity.XpThreshold = spec.XpThreshold;
        entity.NameKey = spec.NameKey.Trim();
        entity.HandItemId = spec.HandItemId;
        entity.CatchMultiplier = spec.CatchMultiplier;
        entity.GoldenMultiplier = spec.GoldenMultiplier;
        entity.HookHavocChance = spec.HookHavocChance;
    }

    /// <summary>
    /// A zone is keyed by its furni class, so two zones on one class is a unique-index violation at
    /// best and an ambiguous lookup at worst. Caught here rather than as a database error.
    /// </summary>
    private static Task<bool> ZoneClassTakenAsync(
        VortexDbContext dbCtx,
        string furniClass,
        int? excludingId,
        CancellationToken ct
    ) =>
        dbCtx.FishingZones.AnyAsync(
            z => z.FurniClass == furniClass.Trim() && (excludingId == null || z.Id != excludingId),
            ct
        );

    private static string? ValidateZone(FishingZoneSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.NameKey) || string.IsNullOrWhiteSpace(spec.FurniClass))
        {
            return "name_and_class_required";
        }

        // A spot that can be emptied in zero catches is a spot nobody can fish, and a maximum below
        // the minimum makes the stock roll throw rather than misbehave quietly.
        return spec.MinCatches <= 0 || spec.MaxCatches < spec.MinCatches ? "invalid_catches" : null;
    }

    private static string? ValidateSpecies(FishingSpeciesSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.NameKey))
        {
            return "name_required";
        }

        if (spec.CatchRate is < 0 or > 1000)
        {
            return "invalid_catch_rate";
        }

        // Weight zero means the species is in the table and can never be picked, which reads as a
        // broken fish rather than a disabled one.
        return spec.RarityWeight <= 0 || spec.MaxWeight < spec.MinWeight ? "invalid_weights" : null;
    }

    private static string? ValidateRodTier(FishingRodTierSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.NameKey) || spec.Quality <= 0)
        {
            return "quality_and_name_required";
        }

        return spec.CatchMultiplier <= 0 || spec.GoldenMultiplier <= 0
            ? "invalid_multipliers"
            : null;
    }

    private static string? ValidateLevel(FishingLevelSpec spec) =>
        spec.Level <= 0 || spec.XpThreshold < 0 ? "invalid_level" : null;

    private async Task<FishingAdminResult> DeleteAsync<TEntity>(int id, CancellationToken ct)
        where TEntity : Database.Entities.VortexEntity =>
        await WriteAsync<object?>(
                null,
                _ => null,
                async (dbCtx, c) =>
                {
                    TEntity? entity = await dbCtx
                        .Set<TEntity>()
                        .FirstOrDefaultAsync(e => e.Id == id, c)
                        .ConfigureAwait(false);

                    if (entity is null)
                    {
                        return null;
                    }

                    dbCtx.Set<TEntity>().Remove(entity);

                    return entity;
                },
                "not_found",
                ct
            )
            .ConfigureAwait(false);

    /// <summary>
    /// The shape every write here shares: validate, do the work, commit, then reload the live
    /// definitions. Written once because the reload is the step easiest to forget, and forgetting it
    /// is invisible — the row is right and the hotel keeps playing the old numbers.
    /// </summary>
    private async Task<FishingAdminResult> WriteAsync<TSpec>(
        TSpec spec,
        Func<TSpec, string?> validate,
        Func<VortexDbContext, CancellationToken, Task<object?>> work,
        string refusal,
        CancellationToken ct
    )
    {
        if (spec is not null && validate(spec) is { } invalid)
        {
            return FishingAdminResult.Fail(invalid);
        }

        await using VortexDbContext dbCtx = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        object? written = await work(dbCtx, ct).ConfigureAwait(false);

        if (written is null)
        {
            return FishingAdminResult.Fail(refusal);
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        int rowId = written is Database.Entities.VortexEntity entity ? entity.Id : 0;

        return FishingAdminResult.Ok(rowId, await ReloadDefinitionsAsync(ct).ConfigureAwait(false));
    }

    private Task<int> ReloadDefinitionsAsync(CancellationToken ct) =>
        grainFactory.GetFishingDefinitionsGrain().ReloadAsync(ct);
}
