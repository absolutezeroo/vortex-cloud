using System;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Groups;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Snapshots;

namespace Vortex.Social.Groups;

/// <summary>
/// The single place a <see cref="GroupMemberSnapshot"/> is built. Five call sites in
/// <c>GroupGrain</c> each repeated this shape by hand, which is how the owner check ended up applied
/// in some of them and not others. Pure entity-in/snapshot-out, so the conventions the client
/// depends on — the role numbering and the join-date format — are testable without a database or a
/// silo.
/// </summary>
internal static class GroupMemberMapper
{
    /// <summary>
    /// The client treats this purely as a display string, but it parses nothing else, so the
    /// day-first layout is fixed.
    /// </summary>
    private const string MemberSinceFormat = "dd-MM-yyyy";

    /// <summary>Maps a persisted membership row. Requires <c>PlayerEntity</c> to be loaded.</summary>
    public static GroupMemberSnapshot FromMember(GroupMemberEntity member, int ownerPlayerId) =>
        new()
        {
            RoleType = RoleFor(member.PlayerEntityId, member.Rank, ownerPlayerId),
            UserId = member.PlayerEntityId,
            UserName = member.PlayerEntity.Name,
            Figure = member.PlayerEntity.Figure,
            MemberSince = member.CreatedAt.ToString(MemberSinceFormat),
        };

    /// <summary>Maps a pending join request. Requires <c>PlayerEntity</c> to be loaded.</summary>
    public static GroupMemberSnapshot FromRequest(GroupMembershipRequestEntity request) =>
        new()
        {
            RoleType = GroupMemberRoles.Requested,
            UserId = request.PlayerEntityId,
            UserName = request.PlayerEntity.Name,
            Figure = request.PlayerEntity.Figure,
            MemberSince = request.CreatedAt.ToString(MemberSinceFormat),
        };

    /// <summary>
    /// A membership that was just created, before any row has been read back — the caller supplies
    /// the timestamp rather than the mapper reading the clock, so the result stays deterministic.
    /// </summary>
    public static GroupMemberSnapshot NewMember(PlayerEntity player, DateTime joinedUtc) =>
        new()
        {
            RoleType = GroupMemberRoles.Member,
            UserId = player.Id,
            UserName = player.Name,
            Figure = player.Figure,
            MemberSince = joinedUtc.ToString(MemberSinceFormat),
        };

    /// <summary>A join request that was just created. See <see cref="NewMember"/>.</summary>
    public static GroupMemberSnapshot NewRequest(PlayerEntity player, DateTime requestedUtc) =>
        new()
        {
            RoleType = GroupMemberRoles.Requested,
            UserId = player.Id,
            UserName = player.Name,
            Figure = player.Figure,
            MemberSince = requestedUtc.ToString(MemberSinceFormat),
        };

    /// <summary>
    /// Owner outranks admin rank: the owner is stored as an ordinary member row, so reading the rank
    /// alone reports them as a plain member and the client then offers to kick them.
    /// </summary>
    public static int RoleFor(int playerId, GroupMemberRank rank, int ownerPlayerId) =>
        playerId == ownerPlayerId ? GroupMemberRoles.Owner
        : rank == GroupMemberRank.Admin ? GroupMemberRoles.Admin
        : GroupMemberRoles.Member;
}
