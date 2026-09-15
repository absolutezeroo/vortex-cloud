using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Protocol.Messages.Incoming.Room.Pets;

public record BreedPetsMessage : IMessageEvent
{
    /// <summary>
    /// Leading field. It was not read at all until 2026-09-15, so the two ids below were shifted by
    /// one: PetOneId held this action code and PetTwoId held the first pet. Every breeding request
    /// therefore looked up a pet with id 0, 1 or 2 and failed with "pet not found".
    /// </summary>
    public required PetBreedingActionType Action { get; init; }

    public required int PetOneId { get; init; }
    public required int PetTwoId { get; init; }
}
