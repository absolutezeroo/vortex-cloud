using System.Collections.Generic;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Navigator;

namespace Vortex.Protocol.Messages.Outgoing.NewNavigator;

public sealed record NavigatorSavedSearchesMessage : IComposer
{
    public required List<NavigatorQuickLinkSnapshot> SavedSearches { get; init; }
}
