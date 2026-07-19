using Orleans;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Pets;

[GenerateSerializer, Immutable]
public sealed record PetCreateRequest
{
    [Id(0)]
    public required string Name { get; init; }

    [Id(1)]
    public required int Type { get; init; }

    [Id(2)]
    public required int Race { get; init; }

    [Id(3)]
    public required string Color { get; init; }

    [Id(4)]
    public required AvatarGenderType Gender { get; init; }

    [Id(5)]
    public required int Energy { get; init; }

    [Id(6)]
    public required int Nutrition { get; init; }
}
