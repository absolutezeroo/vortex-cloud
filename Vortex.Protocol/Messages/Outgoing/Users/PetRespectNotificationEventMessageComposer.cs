using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record PetRespectNotificationEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PetRespect { get; init; }

    [Id(1)]
    public required int PetOwnerId { get; init; }

    [Id(2)]
    public required int PetId { get; init; }

    [Id(3)]
    public required string PetName { get; init; }

    [Id(4)]
    public required int PetType { get; init; }

    [Id(5)]
    public int PetPaletteId { get; init; } = 1;

    [Id(6)]
    public required string PetColor { get; init; }

    [Id(7)]
    public required int PetRace { get; init; }

    [Id(8)]
    public required int PetLevel { get; init; }

    /// <summary>Closes the pet block. The client reads it unconditionally right after the level, so
    /// omitting it leaves the packet four bytes short of what the parser consumes.</summary>
    [Id(9)]
    public int PetRarityLevel { get; init; } = 1;
}
