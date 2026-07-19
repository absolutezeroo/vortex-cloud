using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.Catalog;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record BundleDiscountRulesetMessageComposer : IComposer
{
    [Id(0)]
    public required BundleDiscountRulesetSnapshot BundleDiscountRuleset { get; init; }
}
