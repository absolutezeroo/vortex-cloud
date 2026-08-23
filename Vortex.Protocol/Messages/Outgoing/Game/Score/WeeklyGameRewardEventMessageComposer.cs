using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Game.Score;

[GenerateSerializer, Immutable]
public sealed record WeeklyGameRewardEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
