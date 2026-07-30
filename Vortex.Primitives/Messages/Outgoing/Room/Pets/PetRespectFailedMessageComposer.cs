using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetRespectFailedMessageComposer : IComposer
{
    /// <summary>How old an account must be to respect a pet, and how old this one is -- the client
    /// puts both numbers in the refusal it shows.</summary>
    [Id(0)]
    public required int RequiredDays { get; init; }

    [Id(1)]
    public required int AvatarAgeInDays { get; init; }
}
