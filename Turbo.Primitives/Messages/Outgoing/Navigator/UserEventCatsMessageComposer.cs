using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans.Snapshots.Navigator;

namespace Turbo.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record UserEventCatsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<NavigatorEventCategorySnapshot> EventCategories { get; init; }
}
