using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Availability;

[GenerateSerializer, Immutable]
public sealed record LoginFailedHotelClosedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
