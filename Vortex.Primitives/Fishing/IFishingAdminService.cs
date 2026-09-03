using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Fishing.Admin;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// The fishing content tables, as an operator edits them.
/// </summary>
/// <remarks>
/// <para>
/// Every number in these four tables is a guess reconstructed from Habbo Origins, which has no
/// client dump. That is precisely why they are rows and not code — and until this existed, retuning
/// one still meant hand-written SQL, which is the same as not being tunable.
/// </para>
/// <para>
/// Every write reloads <see cref="Grains.IFishingDefinitionsGrain" />, which both rebuilds the cache
/// and pushes the new definitions to everyone currently fishing. A player mid-session sees the
/// change without reconnecting, and that push is the reason a write must never go straight to the
/// database.
/// </para>
/// </remarks>
public interface IFishingAdminService
{
    Task<FishingAdminResult> CreateZoneAsync(FishingZoneSpec spec, CancellationToken ct);
    Task<FishingAdminResult> UpdateZoneAsync(
        int zoneId,
        FishingZoneSpec spec,
        CancellationToken ct
    );

    /// <summary>
    /// Deletes a zone, refusing while any species still points at it — an orphaned species is a
    /// fish that can never be drawn and nothing would say so.
    /// </summary>
    Task<FishingAdminResult> DeleteZoneAsync(int zoneId, CancellationToken ct);

    Task<FishingAdminResult> CreateSpeciesAsync(FishingSpeciesSpec spec, CancellationToken ct);
    Task<FishingAdminResult> UpdateSpeciesAsync(
        int speciesId,
        FishingSpeciesSpec spec,
        CancellationToken ct
    );
    Task<FishingAdminResult> DeleteSpeciesAsync(int speciesId, CancellationToken ct);

    Task<FishingAdminResult> CreateRodTierAsync(FishingRodTierSpec spec, CancellationToken ct);
    Task<FishingAdminResult> UpdateRodTierAsync(
        int tierId,
        FishingRodTierSpec spec,
        CancellationToken ct
    );
    Task<FishingAdminResult> DeleteRodTierAsync(int tierId, CancellationToken ct);

    Task<FishingAdminResult> CreateLevelAsync(FishingLevelSpec spec, CancellationToken ct);
    Task<FishingAdminResult> UpdateLevelAsync(
        int levelId,
        FishingLevelSpec spec,
        CancellationToken ct
    );
    Task<FishingAdminResult> DeleteLevelAsync(int levelId, CancellationToken ct);

    /// <summary>Rebuilds the live definitions and pushes them to everyone fishing right now.</summary>
    Task<FishingAdminResult> ReloadAsync(CancellationToken ct);
}
