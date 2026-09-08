using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Grains;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Rooms.Tests.Grains;

/// <summary>
/// The guild actions a hotel operator takes from the dashboard, which nobody in the guild is
/// necessarily willing to take.
/// </summary>
/// <remarks>
/// Driven through real grain references for the same reason the authorization tests are: what is
/// being asserted is that these skip the guild's rank check and <em>nothing else</em>, and "nothing
/// else" means the row, the scope check and the event, on the path the dashboard actually calls.
/// </remarks>
[Collection(VortexClusterCollection.Name)]
public sealed class GroupGrainStaffActionsTests(VortexClusterFixture cluster)
{
    private const int StaffId = 4300;

    private readonly VortexClusterFixture _cluster = cluster;

    [Fact]
    public async Task AnOperator_ApprovesARequest_WithoutBeingInTheGuild()
    {
        Ids ids = await SeedAsync(4310).ConfigureAwait(true);

        bool ok = await Group(ids.GroupId)
            .StaffMemberActionAsync(
                StaffId,
                ids.RequesterId,
                GroupStaffAction.ApproveRequest,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ok.Should().BeTrue();

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);

        (
            await db.GroupMembers.AnyAsync(m =>
                m.GroupEntityId == ids.GroupId
                && m.PlayerEntityId == ids.RequesterId
                && m.DeletedAt == null
            )
        )
            .Should()
            .BeTrue();

        (
            await db.GroupMembershipRequests.AnyAsync(r =>
                r.GroupEntityId == ids.GroupId && r.PlayerEntityId == ids.RequesterId
            )
        )
            .Should()
            .BeFalse("the request is answered, not left standing beside the membership");
    }

    [Fact]
    public async Task TheOwner_CannotBeRemovedByAnOperatorEither()
    {
        // The guild's own admins cannot remove the owner, and staff authority does not change what
        // the guild would be left as: a guild with no owner has nobody who can disband or repair it.
        // Disbanding is the operation for that, and it is offered separately.
        Ids ids = await SeedAsync(4320).ConfigureAwait(true);

        bool ok = await Group(ids.GroupId)
            .StaffMemberActionAsync(
                StaffId,
                ids.OwnerId,
                GroupStaffAction.Kick,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ok.Should().BeFalse();

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);

        (
            await db.GroupMembers.AnyAsync(m =>
                m.GroupEntityId == ids.GroupId
                && m.PlayerEntityId == ids.OwnerId
                && m.DeletedAt == null
            )
        )
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task AnOperator_HidesAPostAsStaff_AndTheStateSaysWhoDidIt()
    {
        Ids ids = await SeedAsync(4330).ConfigureAwait(true);

        bool ok = await Forum(ids.GroupId)
            .StaffModeratePostAsync(
                StaffId,
                ids.PostId,
                ForumStaffAction.Hide,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ok.Should().BeTrue();

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);
        GroupForumPostEntity post = await db.GroupForumPosts.SingleAsync(p => p.Id == ids.PostId);

        // Not Hidden: that is what a guild moderator writes. Both vanish from every read, and the
        // difference is the whole record of which of the two happened.
        post.State.Should().Be(GroupForumPostState.HiddenByAdmin);
        post.AdminPlayerEntityId.Should().Be(StaffId);
        post.AdminOperationAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ADeletedPost_IsGoneFromEveryForumRead()
    {
        Ids ids = await SeedAsync(4340).ConfigureAwait(true);

        await Forum(ids.GroupId)
            .StaffModeratePostAsync(
                StaffId,
                ids.PostId,
                ForumStaffAction.Delete,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);
        // The plain query answers nothing at all here, and that IS the assertion: every VortexEntity
        // carries a global `DeletedAt == null` filter, so a deleted post is gone from every read in
        // the hotel without any query saying so. Soft and deliberately so -- an operator who deleted
        // the wrong post has not destroyed the evidence of what it said, and GuildReads.ThreadAsync
        // is the one place that opts out to show it.
        (await db.GroupForumPosts.AnyAsync(p => p.Id == ids.PostId))
            .Should()
            .BeFalse();

        GroupForumPostEntity post = await db
            .GroupForumPosts.IgnoreQueryFilters()
            .SingleAsync(p => p.Id == ids.PostId);

        post.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AThreadFromAnotherGuild_IsRefused()
    {
        // The guild id comes from the dashboard's own request body. Without the scope check, an id
        // typed into the wrong field would moderate a thread in a guild the operator was not
        // looking at -- and the audit row would name the guild they thought they were in.
        Ids first = await SeedAsync(4350).ConfigureAwait(true);
        Ids second = await SeedAsync(4360).ConfigureAwait(true);

        bool ok = await Forum(second.GroupId)
            .StaffModerateThreadAsync(
                StaffId,
                first.ThreadId,
                ForumStaffAction.Hide,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ok.Should().BeFalse();

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);
        GroupForumThreadEntity thread = await db.GroupForumThreads.SingleAsync(t =>
            t.Id == first.ThreadId
        );

        thread.State.Should().Be(GroupForumThreadState.Open);
    }

    [Fact]
    public async Task AHiddenThread_CanBePutBack()
    {
        Ids ids = await SeedAsync(4370).ConfigureAwait(true);
        IGroupForumGrain forum = Forum(ids.GroupId);

        await forum
            .StaffModerateThreadAsync(
                StaffId,
                ids.ThreadId,
                ForumStaffAction.Hide,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await forum
            .StaffModerateThreadAsync(
                StaffId,
                ids.ThreadId,
                ForumStaffAction.Restore,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);
        GroupForumThreadEntity thread = await db.GroupForumThreads.SingleAsync(t =>
            t.Id == ids.ThreadId
        );

        // A hide an operator regrets is one click back, which is the reason hiding is offered
        // beside deleting rather than instead of it.
        thread.State.Should().Be(GroupForumThreadState.Open);
    }

    private IGroupGrain Group(int groupId) => _cluster.GrainFactory.GetGrain<IGroupGrain>(groupId);

    private IGroupForumGrain Forum(int groupId) =>
        _cluster.GrainFactory.GetGrain<IGroupForumGrain>(groupId);

    /// <summary>Ids of one seeded guild. Each test seeds its own so a kick in one is not a missing
    /// row in another -- the silo and its store are shared across the collection.</summary>
    private readonly record struct Ids(
        int GroupId,
        int OwnerId,
        int MemberId,
        int RequesterId,
        int ThreadId,
        int PostId
    );

    private async Task<Ids> SeedAsync(int seed)
    {
        Ids ids = new(seed, seed + 1, seed + 2, seed + 3, seed + 4, seed + 5);

        await using VortexDbContext db = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);

        if (await db.Groups.AnyAsync(g => g.Id == ids.GroupId).ConfigureAwait(true))
        {
            return ids;
        }

        PlayerEntity owner = Player(ids.OwnerId);
        PlayerEntity member = Player(ids.MemberId);
        PlayerEntity requester = Player(ids.RequesterId);

        GroupEntity group = new()
        {
            Id = ids.GroupId,
            Name = $"Guild {ids.GroupId}",
            Badge = "b0501Xs09114s05121",
            RoomEntityId = ids.GroupId + 900,
            OwnerPlayerEntityId = ids.OwnerId,
            Type = GroupType.Exclusive,
            ColorOne = "1",
            ColorTwo = "2",
            AdminOnlyDecoration = false,
            // See GroupGrainMembersAuthorizationTests: RoomEntity carries 29 required members and
            // nothing on these paths reads it.
            RoomEntity = null!,
            OwnerPlayerEntity = owner,
        };

        foreach (PlayerEntity player in new[] { owner, member, requester })
        {
            db.Players.Add(player);
        }

        db.Groups.Add(group);
        db.GroupMembers.Add(Membership(group, owner));
        db.GroupMembers.Add(Membership(group, member));

        db.GroupMembershipRequests.Add(
            new GroupMembershipRequestEntity
            {
                GroupEntityId = ids.GroupId,
                PlayerEntityId = ids.RequesterId,
                GroupEntity = group,
                PlayerEntity = requester,
            }
        );

        GroupForumThreadEntity thread = new()
        {
            Id = ids.ThreadId,
            GroupEntityId = ids.GroupId,
            PlayerEntityId = ids.MemberId,
            Subject = "A thread",
            State = GroupForumThreadState.Open,
            IsPinned = false,
            PostCount = 1,
            GroupEntity = group,
            PlayerEntity = member,
        };

        db.GroupForumThreads.Add(thread);
        db.GroupForumPosts.Add(
            new GroupForumPostEntity
            {
                Id = ids.PostId,
                ThreadEntityId = ids.ThreadId,
                GroupEntityId = ids.GroupId,
                PlayerEntityId = ids.MemberId,
                Message = "Something worth reporting",
                State = GroupForumPostState.Visible,
                ThreadEntity = thread,
                GroupEntity = group,
                PlayerEntity = member,
            }
        );

        await db.SaveChangesAsync().ConfigureAwait(true);

        return ids;
    }

    private static GroupMemberEntity Membership(GroupEntity group, PlayerEntity player) =>
        new()
        {
            GroupEntityId = group.Id,
            PlayerEntityId = player.Id,
            Rank = GroupMemberRank.Member,
            GroupEntity = group,
            PlayerEntity = player,
        };

    private static PlayerEntity Player(int id) =>
        new()
        {
            Id = id,
            Name = $"player-{id}",
            Figure = "hd-180-1",
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };
}
