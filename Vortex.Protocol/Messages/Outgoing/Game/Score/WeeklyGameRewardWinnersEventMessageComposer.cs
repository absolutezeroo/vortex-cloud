using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Game.Score;

[GenerateSerializer, Immutable]
public sealed record WeeklyGameRewardWinnersEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
