using Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Primitives.Pets.Snapshots;

[GenerateSerializer, Immutable]
public sealed record PetSnapshot
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public required PlayerId OwnerId { get; init; }

    [Id(2)]
    public required int? RoomId { get; init; }

    [Id(3)]
    public required string Name { get; init; }

    [Id(4)]
    public required int Type { get; init; }

    [Id(5)]
    public required int Race { get; init; }

    [Id(6)]
    public required string Color { get; init; }

    [Id(7)]
    public required AvatarGenderType Gender { get; init; }

    [Id(8)]
    public required int Level { get; init; }

    [Id(9)]
    public required int Experience { get; init; }

    [Id(10)]
    public required int Energy { get; init; }

    [Id(11)]
    public required int Nutrition { get; init; }

    [Id(12)]
    public required int Respect { get; init; }

    [Id(13)]
    public required int X { get; init; }

    [Id(14)]
    public required int Y { get; init; }

    [Id(15)]
    public required double Z { get; init; }

    [Id(16)]
    public required Rotation Direction { get; init; }
}
