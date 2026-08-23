using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record ChatReviewSessionDetachedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
