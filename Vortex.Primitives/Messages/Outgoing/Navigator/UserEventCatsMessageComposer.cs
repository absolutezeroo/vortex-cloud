using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record UserEventCatsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<NavigatorEventCategorySnapshot> EventCategories { get; init; }
}
