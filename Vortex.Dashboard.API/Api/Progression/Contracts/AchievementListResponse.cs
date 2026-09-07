using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The achievement catalogue, with the ladder and the progress on it.
/// </summary>
/// <param name="Categories">Every category in the table, not only the ones on this page: it fills
/// the filter, which has to offer what the filter can select.</param>
public sealed record AchievementListResponse(
    int Count,
    IReadOnlyList<string> Categories,
    IReadOnlyList<AchievementListItem> Items
);
