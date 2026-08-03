using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Groups;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Grains;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Rooms.Tests.Grains;

/// <summary>
/// The guild members window asks for pending join requests by putting a search type on the wire. The
/// server used to answer that question for anyone who asked and merely tell the client, via
/// <c>AllowedToManage</c>, whether it should have — which disarmed the real client's own UI and
/// nothing else, so a forged packet enumerated the people waiting to join any guild.
/// </summary>
/// <remarks>
/// Deliberately driven through a real grain reference rather than a hand-constructed grain: the
/// refusal has to hold on the path a client actually reaches, arguments and all, and that is the
/// path a silo exercises.
/// </remarks>
[Collection(VortexClusterCollection.Name)]
public sealed class GroupGrainMembersAuthorizationTests(VortexClusterFixture cluster)
{
    private const int GroupId = 4100;
    private const int OwnerId = 4101;
    private const int AdminId = 4102;
    private const int MemberId = 4103;
    private const int RequesterId = 4104;

    private readonly VortexClusterFixture _cluster = cluster;

    [Fact]
    public async Task APlainMember_CannotListThePendingJoinRequests()
    {
        await SeedAsync().ConfigureAwait(true);

        GroupMembersPageSnapshot? page = await Group()
            .GetMembersAsync(
                (PlayerId)MemberId,
                pageIndex: 0,
                userNameFilter: string.Empty,
                searchType: 1,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        page.Should().BeNull("a member who cannot administer the guild must not see who applied");
    }

    [Fact]
    public async Task AnAdmin_CanListThePendingJoinRequests()
    {
        await SeedAsync().ConfigureAwait(true);

        GroupMembersPageSnapshot? page = await Group()
            .GetMembersAsync(
                (PlayerId)AdminId,
                pageIndex: 0,
                userNameFilter: string.Empty,
                searchType: 1,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        page.Should().NotBeNull();
        page!.AllowedToManage.Should().BeTrue();
        page.Members.Should().ContainSingle(m => m.UserId == RequesterId);
        page.Members[0].RoleType.Should().Be(GroupMemberRoles.Requested);
    }

    [Fact]
    public async Task TheOwner_CanListThePendingJoinRequests()
    {
        // The owner holds no admin rank row — they are stored as an ordinary member — so this is the
        // case an authorization check written against the rank alone gets wrong.
        await SeedAsync().ConfigureAwait(true);

        GroupMembersPageSnapshot? page = await Group()
            .GetMembersAsync(
                (PlayerId)OwnerId,
                pageIndex: 0,
                userNameFilter: string.Empty,
                searchType: 1,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        page.Should().NotBeNull();
        page!.Members.Should().ContainSingle(m => m.UserId == RequesterId);
    }

    [Fact]
    public async Task TheOrdinaryMemberList_StaysReadableByAnyMember()
    {
        // The gate is on the pending-requests query only; the roster itself is not a secret, and
        // closing it would blank the window for everyone who is not an admin.
        await SeedAsync().ConfigureAwait(true);

        GroupMembersPageSnapshot? page = await Group()
            .GetMembersAsync(
                (PlayerId)MemberId,
                pageIndex: 0,
                userNameFilter: string.Empty,
                searchType: 0,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        page.Should().NotBeNull();
        page!.AllowedToManage.Should().BeFalse();
        page.Members.Select(m => m.UserId).Should().BeEquivalentTo([OwnerId, AdminId, MemberId]);
        page.Members.Single(m => m.UserId == OwnerId).RoleType.Should().Be(GroupMemberRoles.Owner);
    }

    private IGroupGrain Group() => _cluster.GrainFactory.GetGrain<IGroupGrain>(GroupId);

    /// <summary>
    /// Idempotent: the silo is shared across the collection, so every test seeds the same rows and
    /// the first one to run wins.
    /// </summary>
    private async Task SeedAsync()
    {
        await using VortexDbContext dbCtx = await _cluster
            .Db.CreateDbContextAsync()
            .ConfigureAwait(true);

        if (await dbCtx.Groups.AnyAsync(g => g.Id == GroupId).ConfigureAwait(true))
        {
            return;
        }

        PlayerEntity owner = Player(OwnerId);
        PlayerEntity admin = Player(AdminId);
        PlayerEntity member = Player(MemberId);
        PlayerEntity requester = Player(RequesterId);

        GroupEntity group = new()
        {
            Id = GroupId,
            Name = "Cluster Guild",
            Badge = "b0501Xs09114s05121",
            RoomEntityId = 4200,
            OwnerPlayerEntityId = OwnerId,
            Type = GroupType.Exclusive,
            ColorOne = "1",
            ColorTwo = "2",
            AdminOnlyDecoration = false,
            // Not read by the members query, and RoomEntity carries 29 required members of its own.
            RoomEntity = null!,
            OwnerPlayerEntity = owner,
        };

        foreach (PlayerEntity player in new[] { owner, admin, member, requester })
        {
            dbCtx.Players.Add(player);
        }

        dbCtx.Groups.Add(group);

        dbCtx.GroupMembers.Add(Membership(group, owner, GroupMemberRank.Member));
        dbCtx.GroupMembers.Add(Membership(group, admin, GroupMemberRank.Admin));
        dbCtx.GroupMembers.Add(Membership(group, member, GroupMemberRank.Member));

        dbCtx.GroupMembershipRequests.Add(
            new GroupMembershipRequestEntity
            {
                GroupEntityId = GroupId,
                PlayerEntityId = RequesterId,
                GroupEntity = group,
                PlayerEntity = requester,
            }
        );

        await dbCtx.SaveChangesAsync().ConfigureAwait(true);
    }

    private static GroupMemberEntity Membership(
        GroupEntity group,
        PlayerEntity player,
        GroupMemberRank rank
    ) =>
        new()
        {
            GroupEntityId = group.Id,
            PlayerEntityId = player.Id,
            Rank = rank,
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
