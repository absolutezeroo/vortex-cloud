using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Lobby;

[GenerateSerializer, Immutable]
public sealed record AchievementResolutionProgressMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
