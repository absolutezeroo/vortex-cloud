using Orleans;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Bots;

/// <summary>
/// What a catalog purchase needs to mint a bot. The name comes from the player (they type it at
/// the till, as they do for a pet); the look comes from the product, so a hotel decides what its
/// bots look like rather than the buyer.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record BotCreateRequest
{
    [Id(0)]
    public required string Name { get; init; }

    [Id(1)]
    public required string Figure { get; init; }

    [Id(2)]
    public required AvatarGenderType Gender { get; init; }

    [Id(3)]
    public string Motto { get; init; } = string.Empty;
}
