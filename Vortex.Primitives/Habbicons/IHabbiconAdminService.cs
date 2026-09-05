using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Habbicons.Admin;

namespace Vortex.Primitives.Habbicons;

/// <summary>
/// The management surface for Habbicon content and for one player's Habbicons.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately UI-agnostic: it speaks specs and results, not HTTP. The dashboard is one caller;
/// a console command or a migration is as valid a caller, and none of them is named here.
/// </para>
/// <para>
/// Every write reloads the in-process catalog, and every player-facing write goes through
/// <c>IPlayerHabbiconGrain</c> rather than the database — the grain caches ownership, so a raw
/// update would be invisible to a player who is online and would be overwritten by the grain's
/// next write.
/// </para>
/// </remarks>
public interface IHabbiconAdminService
{
    Task<HabbiconAdminResult> CreateCollectionAsync(
        HabbiconCollectionSpec spec,
        CancellationToken ct
    );

    Task<HabbiconAdminResult> UpdateCollectionAsync(
        int collectionId,
        HabbiconCollectionSpec spec,
        CancellationToken ct
    );

    /// <summary>
    /// Deletes a collection and every Habbicon in it. Refuses while any player owns one of them —
    /// the ownership rows would outlive the definition and show as blanks in their album.
    /// </summary>
    Task<HabbiconAdminResult> DeleteCollectionAsync(int collectionId, CancellationToken ct);

    Task<HabbiconAdminResult> CreateHabbiconAsync(HabbiconSpec spec, CancellationToken ct);

    Task<HabbiconAdminResult> UpdateHabbiconAsync(
        int habbiconId,
        HabbiconSpec spec,
        CancellationToken ct
    );

    /// <summary>Deletes one Habbicon. Refuses while any player owns it.</summary>
    Task<HabbiconAdminResult> DeleteHabbiconAsync(int habbiconId, CancellationToken ct);

    /// <summary>Ownership and completion counts per collection, for the content list.</summary>
    Task<IReadOnlyList<HabbiconCollectionStats>> GetCollectionStatsAsync(CancellationToken ct);

    /// <summary>Everything one player owns, with where each came from.</summary>
    Task<IReadOnlyList<PlayerHabbiconAdminRow>> GetPlayerHabbiconsAsync(
        int playerId,
        CancellationToken ct
    );

    /// <summary>Hands a Habbicon to a player as an operator grant.</summary>
    Task<HabbiconAdminResult> GrantAsync(int playerId, int habbiconId, CancellationToken ct);

    /// <summary>Takes one back. Rare, and audited like every other admin write.</summary>
    Task<HabbiconAdminResult> RevokeAsync(int playerId, int habbiconId, CancellationToken ct);

    /// <summary>
    /// Checks the whole catalog for content problems: duplicate codes, a collection with two
    /// bonuses, a bonus in no collection, an empty set that can never complete, a price in a
    /// currency that does not exist. Reports them all rather than failing on the first.
    /// </summary>
    Task<HabbiconContentReport> ValidateAsync(CancellationToken ct);
}
