using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans.Snapshots.Navigator;

namespace Turbo.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record UserFlatCatsMessageComposer : IComposer
{
    [Id(0)]
    public ImmutableArray<NavigatorFlatCategorySnapshot> Categories { get; init; }
}
