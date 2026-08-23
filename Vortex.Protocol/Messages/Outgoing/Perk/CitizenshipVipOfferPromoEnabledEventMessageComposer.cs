using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Perk;

[GenerateSerializer, Immutable]
public sealed record CitizenshipVipOfferPromoEnabledEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
