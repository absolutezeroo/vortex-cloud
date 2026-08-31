using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Fishing.Grains;

/// <summary>
/// The hotel's fishing definitions: species, zones, rod tiers and the level curve.
/// </summary>
/// <remarks>
/// <para>
/// A kept-alive singleton holding the four tables, because they change when an operator edits them
/// and not while anybody is playing — the same shape as the collections and mystery-box managers.
/// </para>
/// <para>
/// <strong>Reloading is a first-class operation, not a restart.</strong> The whole reason the client
/// receives these as a packet rather than as a gamedata file is that an operator editing a catch
/// rate has to reach a player already standing at a pond. <see cref="ReloadAsync"/> re-reads the
/// tables, bumps the version, and broadcasts; the client drops a push whose version is not newer, so
/// a redundant call costs nothing.
/// </para>
/// </remarks>
public interface IFishingDefinitionsGrain : IGrainWithStringKey
{
    /// <summary>The current tables, hydrated on first use.</summary>
    Task<FishingDefinitionsSnapshot> GetDefinitionsAsync(CancellationToken ct);

    /// <summary>
    /// Re-reads the tables from the database, bumps the version and pushes the result to every
    /// connected session. Answers the new version.
    /// </summary>
    Task<int> ReloadAsync(CancellationToken ct);

    /// <summary>
    /// The zone a spot's furni class belongs to, or null when that class is not a fishing spot.
    /// Answering null is the ordinary case: most furniture is not a pond.
    /// </summary>
    Task<FishingZoneSnapshot?> GetZoneForFurniClassAsync(string furniClass, CancellationToken ct);

    /// <summary>Every species of one zone, unfiltered — the caller applies level, hour and season.</summary>
    Task<ImmutableArray<FishSpeciesSnapshot>> GetSpeciesForZoneAsync(
        int zoneId,
        CancellationToken ct
    );

    /// <summary>
    /// The tunables, cached and reloaded with the tables. Read rather than hardcoded so an operator
    /// can retune the daily cap, the frenzy or Hook Havoc while people are fishing.
    /// </summary>
    Task<FishingSettingsSnapshot> GetSettingsAsync(CancellationToken ct);
}
