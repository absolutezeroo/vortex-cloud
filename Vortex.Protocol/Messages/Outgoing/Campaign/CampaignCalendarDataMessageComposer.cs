using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Campaign;

[GenerateSerializer, Immutable]
public sealed record CampaignCalendarDataMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
