using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>Every track, its content, and how the hotel is doing on it.</summary>
public sealed record RewardTrackList(int Count, IReadOnlyList<RewardTrackRow> Items);
