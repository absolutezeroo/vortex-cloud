using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>
/// Everything the client's <c>HabboGroupDetailsMessageEvent</c> renders. Field set + order is
/// dictated by the Flash client parser (see vortex-client HabboGroupDetailsData).
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupDetailsSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required bool IsGuild { get; init; }

    [Id(2)]
    public required int Type { get; init; }

    [Id(3)]
    public required string Name { get; init; }

    [Id(4)]
    public required string Description { get; init; }

    [Id(5)]
    public required string BadgeCode { get; init; }

    [Id(6)]
    public required int RoomId { get; init; }

    [Id(7)]
    public required string RoomName { get; init; }

    /// <summary>Viewer's membership status: 0 = not member, 1 = member, 2 = request pending.</summary>
    [Id(8)]
    public required int Status { get; init; }

    [Id(9)]
    public required int TotalMembers { get; init; }

    [Id(10)]
    public required bool Favourite { get; init; }

    [Id(11)]
    public required string CreationDate { get; init; }

    [Id(12)]
    public required bool IsOwner { get; init; }

    [Id(13)]
    public required bool IsAdmin { get; init; }

    [Id(14)]
    public required string OwnerName { get; init; }

    /// <summary>Tells the client to open the group details popup. Set from the request flag, not group state.</summary>
    [Id(15)]
    public required bool OpenDetails { get; init; }

    [Id(16)]
    public required bool MembersCanDecorate { get; init; }

    [Id(17)]
    public required int PendingMemberCount { get; init; }

    [Id(18)]
    public required bool HasForum { get; init; }
}
