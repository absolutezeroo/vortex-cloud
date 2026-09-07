using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>The community goals and where the hotel stands on each.</summary>
public sealed record CommunityGoalList(int Count, IReadOnlyList<CommunityGoalRow> Items);
