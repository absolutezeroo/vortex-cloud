using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The mystery box: which furniture can be one, what it can hand out, and at what odds.
/// </summary>
/// <param name="Colors">The colours a box can be, which is a box's own furniture state -- so the
/// editor offers those rather than a free field.</param>
public sealed record MysteryBoxContent(
    MysteryBoxDefinitionList Definitions,
    MysteryBoxPrizeList Prizes,
    IReadOnlyList<MysteryBoxPoolOdds> Pools,
    IReadOnlyList<string> Colors,
    IReadOnlyList<string> ProductTypes
);
