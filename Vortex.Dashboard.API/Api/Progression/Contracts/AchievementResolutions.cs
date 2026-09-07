using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The resolution statues: which achievements they offer, and how the challenges taken on them went.
/// </summary>
/// <param name="Truncated">True when more challenges matched the filter than the page carries. The
/// list is capped rather than paged, so this is how the page knows to say so.</param>
public sealed record AchievementResolutions(
    IReadOnlyList<ResolutionOffer> Offers,
    IReadOnlyList<ResolutionChallenge> Challenges,
    ResolutionTotals Totals,
    bool Truncated
);
