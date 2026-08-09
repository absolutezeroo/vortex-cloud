namespace Vortex.Primitives.Navigator;

/// <summary>
/// Live population of one navigator flat category: how many players are in its rooms right now, and
/// how many those rooms could hold.
/// </summary>
public readonly record struct NavigatorCategoryVisitorCount(int CurrentUserCount, int MaxUserCount);
