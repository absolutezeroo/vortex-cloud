using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Achievements;

[GenerateSerializer, Immutable]
public sealed record AchievementsScoreEventMessageComposer : IComposer
{
    [Id(0)]
    public required int Score { get; init; }
}
