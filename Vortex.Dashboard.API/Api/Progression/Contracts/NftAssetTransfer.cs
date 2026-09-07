using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One hand-over in a Relic's history.
/// </summary>
/// <param name="FromPlayer">Null for the mint itself, which comes from nobody.</param>
public sealed record NftAssetTransfer(
    int Id,
    string? FromPlayer,
    string? ToPlayer,
    string Reason,
    DateTime At
);
