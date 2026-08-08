using Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

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

    /// <summary>Zero while the bot is in the owner's hand; only meaningful once placed.</summary>
    [Id(6)]
    public int X { get; init; }

    [Id(7)]
    public int Y { get; init; }

    [Id(8)]
    public Altitude Z { get; init; }

    [Id(9)]
    public Rotation Rotation { get; init; }
}
