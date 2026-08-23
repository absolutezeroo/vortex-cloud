using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record NestBreedingSuccessEventMessageComposer : IComposer
{
    [Id(0)]
    public required int NewPetId { get; init; }

    /// <summary>Rarity tier of the offspring, read unconditionally by the client. The breeding
    /// system does not compute one yet, so it stays 0.</summary>
    [Id(1)]
    public int RarityCategory { get; init; }
}
