using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetLevelUpdateMessageComposer : IComposer
{
    /// <summary>The pet's room object id. The client looks the pet up in the room by this, so it
    /// has to come first and it is not the pet row id.</summary>
    [Id(0)]
    public required int RoomIndex { get; init; }

    [Id(1)]
    public required int PetId { get; init; }

    [Id(2)]
    public required int Level { get; init; }
}
