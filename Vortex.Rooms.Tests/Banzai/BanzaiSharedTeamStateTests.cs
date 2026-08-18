using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Banzai;

/// <summary>
/// Locks that Banzai's gates write into the room's ONE shared team store — the mirror of
/// <c>FreezeSharedTeamStateTests</c>, because a second store was exactly the bug the seam exists to
/// prevent: every wired team leaf (actor-in-team, team-has-score/rank, users-of-team, score
/// trigger) reads the shared state, so a Banzai player who existed only in a private roster would
/// be invisible to all of them.
/// </summary>
public sealed class BanzaiSharedTeamStateTests
{
    [Fact]
    public async Task AGateJoin_IsVisibleToTheSharedStore_AndWearsTheWiredAura()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        await harness
            .Grain.BanzaiSystem.OnGateWalkOnAsync(
                RoomHarness.Stranger,
                GameTeamColor.Red,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain.GameSystem.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.Red);
        avatar.CurrentEffectId.Should().Be(33, "Banzai wears the wired aura set (32 + team)");
    }

    [Fact]
    public async Task LeavingThroughTheGate_ClearsMembershipAndAura()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        await harness
            .Grain.BanzaiSystem.OnGateWalkOnAsync(
                RoomHarness.Stranger,
                GameTeamColor.Red,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await harness
            .Grain.BanzaiSystem.OnGateWalkOnAsync(
                RoomHarness.Stranger,
                GameTeamColor.Red,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain.GameSystem.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.None);
        avatar.CurrentEffectId.Should().Be(0);
    }

    [Fact]
    public async Task SwitchingGates_MovesTheSharedMembership()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        await harness
            .Grain.BanzaiSystem.OnGateWalkOnAsync(
                RoomHarness.Stranger,
                GameTeamColor.Red,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await harness
            .Grain.BanzaiSystem.OnGateWalkOnAsync(
                RoomHarness.Stranger,
                GameTeamColor.Yellow,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        harness.Grain.GameSystem.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.Yellow);
        harness
            .Grain.GameSystem.GetPlayersInTeam(GameTeamColor.Red)
            .Should()
            .BeEmpty("the old membership moved, it was not duplicated");
        avatar.CurrentEffectId.Should().Be(36);
    }

    [Fact]
    public async Task APlayerLeavingTheRoom_LeavesTheTeamWithIt()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        await harness
            .Grain.BanzaiSystem.OnGateWalkOnAsync(
                RoomHarness.Stranger,
                GameTeamColor.Blue,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        await harness
            .Grain.GameSystem.OnPlayerLeftAsync(RoomHarness.Stranger, CancellationToken.None)
            .ConfigureAwait(true);

        harness.Grain.GameSystem.GetTeam(RoomHarness.Stranger).Should().Be(GameTeamColor.None);
    }
}
