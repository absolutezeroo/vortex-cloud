using Orleans;

namespace Turbo.Primitives.Snapshots.FriendList;

[GenerateSerializer, Immutable]
public record RelationshipStatusEntrySnapshot
{
    [Id(0)]
    public required short RelationType { get; init; }

    [Id(1)]
    public required int Count { get; init; }

    [Id(2)]
    public required string Name { get; init; }

    [Id(3)]
    public required string Figure { get; init; }
}
