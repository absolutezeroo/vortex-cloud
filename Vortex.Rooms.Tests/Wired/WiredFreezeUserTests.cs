using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The avatar movement lock behind the wired freeze-user/unfreeze-user boxes and a Freeze-game hit.
/// The contract: a locked avatar starts no walk (and its in-flight walk is cancelled when the lock
/// lands), unlocking restores movement, and locking someone not in the room is a harmless no-op.
/// The lock lives on the avatar itself, so it cannot outlive the room presence.
/// </summary>
public sealed class WiredFreezeUserTests
{
    [Fact]
    public async Task ALockedAvatar_StartsNoWalk()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        harness.Grain.GameRuntime.Chrome.LockMovement(RoomHarness.Stranger);

        avatar.IsMovementLocked.Should().BeTrue();

        bool walked = await harness
            .Grain.AvatarModule.WalkAvatarToAsync(avatar, 4, 4, CancellationToken.None)
            .ConfigureAwait(true);

        walked.Should().BeFalse("frozen means frozen — no new walk starts");
    }

    [Fact]
    public async Task LockingMidWalk_CancelsTheWalkInFlight()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        await harness
            .Grain.AvatarModule.WalkAvatarToAsync(avatar, 4, 4, CancellationToken.None)
            .ConfigureAwait(true);

        harness.Grain.GameRuntime.Chrome.LockMovement(RoomHarness.Stranger);

        avatar.IsWalking.Should().BeFalse("the in-flight walk is cancelled, not just future ones");
        avatar.TilePath.Should().BeEmpty();
    }

    [Fact]
    public async Task Unlocking_RestoresMovement()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        harness.Grain.GameRuntime.Chrome.LockMovement(RoomHarness.Stranger);

        harness.Grain.GameRuntime.Chrome.UnlockMovement(RoomHarness.Stranger);

        avatar.IsMovementLocked.Should().BeFalse();

        bool walked = await harness
            .Grain.AvatarModule.WalkAvatarToAsync(avatar, 4, 4, CancellationToken.None)
            .ConfigureAwait(true);

        walked.Should().BeTrue();
    }

    [Fact]
    public async Task LockingSomeoneNotInTheRoom_IsANoOp()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.Grain.GameRuntime.Chrome.LockMovement(RoomHarness.Stranger);
        harness.Grain.GameRuntime.Chrome.UnlockMovement(RoomHarness.Stranger);
    }
}
