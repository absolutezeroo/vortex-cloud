using System.Collections.Generic;

namespace Vortex.Primitives.Snapshots.Navigator;

public sealed record CategoriesWithVisitorCountSnapshot(
    Dictionary<int, List<int>> CategoriesWithVisitorCount
);
