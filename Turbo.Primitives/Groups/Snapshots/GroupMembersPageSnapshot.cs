using System.Collections.Generic;
using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>
/// A paginated guild member list as the client's <c>GuildMembersMessageEvent</c> expects it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupMembersPageSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required string GroupName { get; init; }

    [Id(2)]
    public required int BaseRoomId { get; init; }

    [Id(3)]
    public required string BadgeCode { get; init; }

    [Id(4)]
    public required int TotalEntries { get; init; }

    [Id(5)]
    public required List<GroupMemberSnapshot> Members { get; init; }

    [Id(6)]
    public required bool AllowedToManage { get; init; }

    [Id(7)]
    public required int PageSize { get; init; }

    [Id(8)]
    public required int PageIndex { get; init; }

    [Id(9)]
    public required int SearchType { get; init; }

    [Id(10)]
    public required string UserNameFilter { get; init; }
}
