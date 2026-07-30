using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetStatusUpdateMessageComposer : IComposer
{
    /// <summary>The pet's room object id -- what the client finds the pet in the room by.</summary>
    [Id(0)]
    public required int RoomIndex { get; init; }

    [Id(1)]
    public required int PetId { get; init; }

    /// <summary>Which actions the client offers on the pet. Nothing else tells it to show the
    /// breed, harvest and revive buttons.</summary>
    [Id(2)]
    public bool CanBreed { get; init; }

    [Id(3)]
    public bool CanHarvest { get; init; }

    [Id(4)]
    public bool CanRevive { get; init; }

    [Id(5)]
    public bool HasBreedingPermission { get; init; }
}
