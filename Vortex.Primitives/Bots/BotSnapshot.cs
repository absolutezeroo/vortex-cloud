using Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Bots;

/// <summary>A bot as the owner's inventory shows it.</summary>
[GenerateSerializer, Immutable]
public sealed record BotSnapshot
{
    [Id(0)]
    public required int BotId { get; init; }

    [Id(1)]
    public required PlayerId OwnerId { get; init; }

    [Id(2)]
    public required string Name { get; init; }

    [Id(3)]
    public string Motto { get; init; } = string.Empty;

    [Id(4)]
    public required string Figure { get; init; }

    [Id(5)]
    public required AvatarGenderType Gender { get; init; }
}
