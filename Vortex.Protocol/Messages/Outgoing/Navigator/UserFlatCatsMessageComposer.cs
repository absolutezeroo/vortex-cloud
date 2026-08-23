using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record UserFlatCatsMessageComposer : IComposer
{
    [Id(0)]
    public ImmutableArray<NavigatorFlatCategorySnapshot> Categories { get; init; }
}
