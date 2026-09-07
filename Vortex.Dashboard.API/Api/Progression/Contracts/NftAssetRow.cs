using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One minted Relic and where it has been.
/// </summary>
/// <param name="History">The provenance ledger. This is the part of a chain worth keeping: an admin
/// can answer "who had this before" without one.</param>
public sealed record NftAssetRow(
    int Id,
    int PlayerId,
    string? PlayerName,
    string ProductCode,
    int StampCost,
    int SerialNumber,
    int EditionSize,
    DateTime MintedAt,
    string? IconUrl,
    IReadOnlyList<NftAssetTransfer> History
);
