using Orleans;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

/// <summary>
/// Re-badges an avatar already standing in the room after the player changed their favourite guild.
/// AS3-verified against <c>FavouriteMembershipUpdateMessageParser</c>: the client reads
/// <c>roomIndex, habboGroupId, status, habboGroupName</c>, keys off the avatar's room index (its
/// object id, not the player id), and treats <c>habboGroupId == -1</c> as "clear the badge".
/// </summary>
[GenerateSerializer, Immutable]
public sealed record FavoriteMembershipUpdateMessageComposer : IComposer
{
    /// <summary>The target avatar's room object id — NOT the player id.</summary>
    [Id(0)]
    public required int RoomIndex { get; init; }

    /// <summary>Guild whose badge to show, or -1 to clear it.</summary>
    [Id(1)]
    public required int GroupId { get; init; }

    [Id(2)]
    public required GroupMembershipStatus Status { get; init; }

    [Id(3)]
    public required string GroupName { get; init; }
}
