using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room.Furniture;

namespace Vortex.Primitives.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record AreaHideMessageComposer : IComposer
{
    [Id(0)]
    public required AreaHideDataSnapshot AreaHideData { get; init; }
}
