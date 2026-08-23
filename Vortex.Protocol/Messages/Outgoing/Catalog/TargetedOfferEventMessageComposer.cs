using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

/// <summary>The targeted offer currently shown to the player.</summary>
[GenerateSerializer, Immutable]
public sealed record TargetedOfferEventMessageComposer : IComposer
{
    [Id(0)]
    public required TargetedOfferSnapshot Offer { get; init; }
}
