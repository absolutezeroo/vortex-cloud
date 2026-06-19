using System.Collections.Generic;
using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>
/// Payload for the client's <c>GuildEditInfoMessageEvent</c> (the guild manager edit screen).
/// Field set + order follow the Flash client GuildEditData parser.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupEditInfoSnapshot
{
    [Id(0)]
    public required List<GroupRoomSnapshot> OwnedRooms { get; init; }

    [Id(1)]
    public required bool IsOwner { get; init; }

    [Id(2)]
    public required int GroupId { get; init; }

    [Id(3)]
    public required string GroupName { get; init; }

    [Id(4)]
    public required string GroupDescription { get; init; }

    [Id(5)]
    public required int BaseRoomId { get; init; }

    [Id(6)]
    public required int PrimaryColorId { get; init; }

    [Id(7)]
    public required int SecondaryColorId { get; init; }

    [Id(8)]
    public required int GuildType { get; init; }

    [Id(9)]
    public required int GuildRightsLevel { get; init; }

    [Id(10)]
    public required bool Locked { get; init; }

    [Id(11)]
    public required string Url { get; init; }

    [Id(12)]
    public required List<GroupBadgePartSnapshot> BadgeParts { get; init; }

    [Id(13)]
    public required string BadgeCode { get; init; }

    [Id(14)]
    public required int MembershipCount { get; init; }
}
