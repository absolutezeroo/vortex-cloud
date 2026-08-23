using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetPlacingErrorMessageComposer : IComposer
{
    [Id(0)]
    public required int ErrorCode { get; init; }
}
