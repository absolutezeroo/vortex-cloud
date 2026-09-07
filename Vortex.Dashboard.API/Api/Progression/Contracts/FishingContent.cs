using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>The four fishing content tables, read together because each is meaningless alone.</summary>
public sealed record FishingContent(
    IReadOnlyList<FishingZoneRow> Zones,
    IReadOnlyList<FishingSpeciesRow> Species,
    IReadOnlyList<FishingRodTierRow> RodTiers,
    IReadOnlyList<FishingLevelRow> Levels
);
