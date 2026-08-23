using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record OpenPetPackageRequestedMessageComposer : IComposer
{
    [Id(0)]
    public required int ObjectId { get; init; }

    /// <summary>The pet inside the package, so the naming dialog can show what was opened.</summary>
    [Id(1)]
    public required PetSnapshot Pet { get; init; }
}
