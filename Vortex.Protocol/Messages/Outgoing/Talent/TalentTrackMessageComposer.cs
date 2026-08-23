using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Talent;

[GenerateSerializer, Immutable]
public sealed record TalentTrackMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
