using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Competition;

[GenerateSerializer, Immutable]
public sealed record IsUserPartOfCompetitionMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
