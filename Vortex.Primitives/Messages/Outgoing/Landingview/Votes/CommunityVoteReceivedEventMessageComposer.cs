using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Landingview.Votes;

[GenerateSerializer, Immutable]
public sealed record CommunityVoteReceivedEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
