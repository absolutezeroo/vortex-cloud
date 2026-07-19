using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Clothing;

[GenerateSerializer, Immutable]
public sealed record FigureSetIdsEventMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<int> FigureSetIds { get; init; }

    [Id(1)]
    public required ImmutableArray<string> BoundFurnitureNames { get; init; }
}
