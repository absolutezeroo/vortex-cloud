using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Game.Lobby;

[GenerateSerializer, Immutable]
public sealed record UserGameAchievementsMessageMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
