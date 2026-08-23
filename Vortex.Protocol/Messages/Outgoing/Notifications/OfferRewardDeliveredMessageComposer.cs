using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record OfferRewardDeliveredMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
