using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One pool's draws over the window, broken down two ways.</summary>
public sealed record PrizePoolDraws(
    string Pool,
    int Draws,
    IReadOnlyList<PrizeEntryDraws> Entries,
    IReadOnlyList<PrizeSourceDraws> Sources
);
