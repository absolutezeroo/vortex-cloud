using System.Collections.Generic;
using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>
/// Payload for the client's <c>GuildCreationInfoMessageEvent</c>: how much a guild costs plus the
/// rooms the player can attach it to, and the (optional) default badge parts.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupCreationInfoSnapshot
{
    [Id(0)]
    public required int CostInCredits { get; init; }

    [Id(1)]
    public required List<GroupRoomSnapshot> Rooms { get; init; }

    [Id(2)]
    public required List<GroupBadgePartSnapshot> BadgeParts { get; init; }
}
