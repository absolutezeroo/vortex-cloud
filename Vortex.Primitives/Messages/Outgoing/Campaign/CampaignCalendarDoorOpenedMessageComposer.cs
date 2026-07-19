using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Campaign;

[GenerateSerializer, Immutable]
public sealed record CampaignCalendarDoorOpenedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
