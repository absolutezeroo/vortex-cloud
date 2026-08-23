using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record RentableSpaceRentOkMessageComposer : IComposer
{
    [Id(0)]
    public required int ExpiryTime { get; init; }
}
