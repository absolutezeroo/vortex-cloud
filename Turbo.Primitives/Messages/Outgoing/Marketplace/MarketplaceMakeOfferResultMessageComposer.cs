using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketplaceMakeOfferResultMessageComposer : IComposer
{
    [Id(0)] public required int Result { get; init; }
}
