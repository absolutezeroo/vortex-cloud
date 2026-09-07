using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One wearable avatar, and who holds a copy.
/// </summary>
/// <param name="KnownCollection">The caption a player reads under the tile is built from the
/// contract key, and the client has no branch for one it does not know: it prints the literal word
/// "null" instead of a collection name.</param>
public sealed record NftAvatarRow(
    int Id,
    string AvatarCode,
    string Name,
    string Figure,
    string Gender,
    string ContractKey,
    int EditionSize,
    bool Enabled,
    int SortOrder,
    int GrantedCount,
    bool Exhausted,
    bool KnownCollection,
    string? AvatarImageUrl,
    IReadOnlyList<NftAvatarHolder> Holders
);
