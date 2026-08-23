using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.Navigator;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record CategoriesWithVisitorCountMessageComposer : IComposer
{
    [Id(0)]
    public required CategoriesWithVisitorCountSnapshot Categories { get; init; }
}
