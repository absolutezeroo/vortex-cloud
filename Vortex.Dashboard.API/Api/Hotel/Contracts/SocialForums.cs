using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>The guild forums, which are social but not messenger, so they answer under their own key.</summary>
public sealed record SocialForums(
    IReadOnlyList<ForumStateCount> ThreadsByState,
    IReadOnlyList<ForumStateCount> PostsByState,
    IReadOnlyList<ForumGroupRanking> TopGroups,
    IReadOnlyList<ForumThreadSummary> RecentThreads
);
