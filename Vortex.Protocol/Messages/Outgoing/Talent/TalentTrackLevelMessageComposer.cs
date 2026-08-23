using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Talent;

[GenerateSerializer, Immutable]
public sealed record TalentTrackLevelMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
