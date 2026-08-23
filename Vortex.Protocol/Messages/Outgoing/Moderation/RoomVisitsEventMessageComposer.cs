using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Moderation;

/// <summary>
/// Where a user has been, for the mod tool. Despite the name this is keyed by user, not by room —
/// the client's RoomVisitsCtrl sends a user id and renders one row per room they entered.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomVisitsEventMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required string UserName { get; init; }

    [Id(2)]
    public ImmutableArray<RoomVisitSnapshot> Visits { get; init; } =
        ImmutableArray<RoomVisitSnapshot>.Empty;
}
