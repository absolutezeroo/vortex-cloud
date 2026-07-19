using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record BuildersClubFurniCountMessageComposer : IComposer
{
    [Id(0)]
    public required int FurniCount { get; init; }
}
