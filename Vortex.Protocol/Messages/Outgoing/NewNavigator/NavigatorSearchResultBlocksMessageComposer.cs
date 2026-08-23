using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.Primitives.Messages.Outgoing.NewNavigator;

[GenerateSerializer, Immutable]
public sealed record NavigatorSearchResultBlocksMessageComposer : IComposer
{
    [Id(0)]
    public required string SearchCodeOriginal { get; init; }

    [Id(1)]
    public required string FilteringData { get; init; }

    [Id(2)]
    public required ImmutableArray<NavigatorSearchResultBlockSnapshot> Blocks { get; init; }
}
