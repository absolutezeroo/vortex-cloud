using System.Collections.Immutable;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.Primitives.Messages.Outgoing.NewNavigator;

public sealed record NavigatorMetaDataMessage : IComposer
{
    public required ImmutableArray<NavigatorTopLevelContextSnapshot> TopLevelContexts { get; init; }
}
