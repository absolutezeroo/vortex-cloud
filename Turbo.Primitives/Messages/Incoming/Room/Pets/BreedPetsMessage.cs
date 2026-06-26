using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Pets;

public record BreedPetsMessage : IMessageEvent
{
    public required int PetOneId { get; init; }
    public required int PetTwoId { get; init; }
}
