using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>
/// One row in a guild member list. <see cref="RoleType"/> follows the client enum:
/// 0 = owner, 1 = admin, 2 = member, 3 = invited/requested, 4 = blocked.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupMemberSnapshot
{
    [Id(0)]
    public required int RoleType { get; init; }

    [Id(1)]
    public required int UserId { get; init; }

    [Id(2)]
    public required string UserName { get; init; }

    [Id(3)]
    public required string Figure { get; init; }

    [Id(4)]
    public required string MemberSince { get; init; }
}
