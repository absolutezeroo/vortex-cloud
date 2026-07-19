using Orleans;

namespace Vortex.Primitives.Snapshots.FriendList;

[GenerateSerializer, Immutable]
public record FriendCategorySnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Name { get; init; }
}
