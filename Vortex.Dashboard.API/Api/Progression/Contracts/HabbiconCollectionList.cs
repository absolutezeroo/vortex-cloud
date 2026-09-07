using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The Habbicon collections and what they hold.
/// </summary>
/// <param name="Artwork">Where the pictures come from, or null when no pack is installed -- the
/// page then lists codes instead of icons, which is why this is nullable rather than empty.</param>
public sealed record HabbiconCollectionList(
    int Count,
    IReadOnlyList<HabbiconCollectionRow> Items,
    HabbiconSheets? Artwork
);
