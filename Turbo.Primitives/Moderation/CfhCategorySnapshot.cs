using System.Collections.Immutable;

namespace Turbo.Primitives.Moderation;

public readonly record struct CfhCategorySnapshot(
    int Id,
    string Name,
    ImmutableArray<CfhTopicSnapshot> Topics
);
