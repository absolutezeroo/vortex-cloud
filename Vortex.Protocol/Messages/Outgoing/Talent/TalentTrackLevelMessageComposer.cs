using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Talent;

[GenerateSerializer, Immutable]
public sealed record TalentTrackLevelMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
