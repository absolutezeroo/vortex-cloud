using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Quest;

/// <summary>A quest was cancelled/rejected. <see cref="Expired"/> distinguishes an expiry from a
/// user cancellation.</summary>
[GenerateSerializer, Immutable]
public sealed record QuestCancelledMessageComposer : IComposer
{
    [Id(0)]
    public required bool Expired { get; init; }

    [Id(1)]
    public required QuestSnapshot Quest { get; init; }
}
