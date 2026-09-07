using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One thing that may be converted into a Relic.
/// </summary>
/// <param name="Resolved">A type naming furniture that does not exist is dropped by the grain
/// rather than listed -- the client would count the player's copies by a sprite id it has no
/// definition for -- so this flag is the only warning an admin gets.</param>
/// <param name="Open">Enabled and inside its window: what the player can actually convert. The
/// client gives no reason for a closed one.</param>
public sealed record MintableTypeRow(
    int Id,
    string ProductCode,
    int StampPrice,
    DateTime StartsAt,
    DateTime EndsAt,
    bool RegionLocked,
    bool LimitedEdition,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    int MintedCount,
    bool Exhausted,
    bool Resolved,
    bool Open,
    bool Expired,
    bool IsNft,
    string? IconUrl
);
