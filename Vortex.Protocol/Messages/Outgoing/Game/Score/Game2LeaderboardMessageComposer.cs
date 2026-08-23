using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Score;

[GenerateSerializer, Immutable]
public sealed record Game2LeaderboardMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
