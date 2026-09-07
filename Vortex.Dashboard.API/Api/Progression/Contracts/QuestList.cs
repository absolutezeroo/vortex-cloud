using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The quest catalogue.
/// </summary>
/// <param name="Campaigns">Every campaign present in the answer, so the filter offers what it can
/// select.</param>
public sealed record QuestList(
    int Count,
    IReadOnlyList<string> Campaigns,
    IReadOnlyList<QuestRow> Items
);
