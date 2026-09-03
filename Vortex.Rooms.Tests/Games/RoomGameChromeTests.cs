using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Protocol.Messages.Outgoing.Room.Action;
using Vortex.Protocol.Messages.Outgoing.Room.Session;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Object.Avatars.Player;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The one copy of the game-facing client plumbing. Before it existed, the coordinator and Freeze
/// each carried their own effect broadcaster — with two different team-aura bases writing to the
/// same shared team store — and the playing-mode / player-value senders lived inside Freeze where
/// the next game could not reach them. These tests pin the chrome's observable contract: effects
/// persist on the avatar (that is what re-syncs a late joiner) and broadcast to the room, the aura
/// arithmetic maps each set to its client effect range, and the playing-mode flag reaches the
/// player's own presence grain.
/// </summary>
public sealed class RoomGameChromeTests
{
    [Fact]
    public async Task BroadcastEffect_PersistsOnTheAvatar_AndBroadcastsToTheRoom()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        await harness
            .Grain.GameRuntime.Chrome.BroadcastEffectAsync(RoomHarness.Stranger, 40)
            .ConfigureAwait(true);

        avatar
            .CurrentEffectId.Should()
            .Be(40, "the snapshot effect is what re-syncs a late joiner's view");
        harness
            .BroadcastToRoom.OfType<AvatarEffectMessageComposer>()
            .Should()
            .ContainSingle()
            .Which.EffectId.Should()
            .Be(40);
    }

    [Fact]
    public async Task BroadcastEffect_ForAPlayerNotInTheRoom_SendsNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.GameRuntime.Chrome.BroadcastEffectAsync(RoomHarness.Stranger, 40)
            .ConfigureAwait(true);

        harness.BroadcastToRoom.Should().BeEmpty();
    }

    [Theory]
    [InlineData(GameAuraSet.Wired, GameTeamColor.Red, 33)]
    [InlineData(GameAuraSet.Wired, GameTeamColor.Yellow, 36)]
    [InlineData(GameAuraSet.Freeze, GameTeamColor.Red, 40)]
    [InlineData(GameAuraSet.Freeze, GameTeamColor.Yellow, 43)]
    public async Task TeamAura_MapsEachSetToItsClientEffectRange(
        GameAuraSet aura,
        GameTeamColor team,
        int expectedEffect
    )
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        await harness
            .Grain.GameRuntime.Chrome.BroadcastTeamAuraAsync(RoomHarness.Stranger, aura, team)
            .ConfigureAwait(true);

        avatar.CurrentEffectId.Should().Be(expectedEffect);
    }

    [Fact]
    public async Task TeamAura_ForNoTeam_ClearsTheEffect()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);
        avatar.SetEffect(40);

        await harness
            .Grain.GameRuntime.Chrome.BroadcastTeamAuraAsync(
                RoomHarness.Stranger,
                GameAuraSet.Freeze,
                GameTeamColor.None
            )
            .ConfigureAwait(true);

        avatar.CurrentEffectId.Should().Be(0);
    }

    [Fact]
    public async Task PlayingMode_ReachesThePlayersOwnPresenceGrain()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        await harness
            .Grain.GameRuntime.Chrome.SetPlayingModeAsync(RoomHarness.Stranger, true)
            .ConfigureAwait(true);

        harness.ComposersSentTo.Should().ContainSingle().Which.Should().Be(RoomHarness.Stranger);
    }

    [Fact]
    public async Task PlayingModeAndForget_TheLeavePathVariant_StillDelivers()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        // Fire-and-forget by contract (the leave path must not await back into the leaver's own
        // presence grain); the fake presence grain completes synchronously, so the send is visible
        // immediately.
        harness.Grain.GameRuntime.Chrome.SetPlayingModeAndForget(RoomHarness.Stranger, false);

        harness.ComposersSentTo.Should().ContainSingle().Which.Should().Be(RoomHarness.Stranger);
    }

    [Fact]
    public async Task PlayerValue_BroadcastsTheNumberBubble()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);
        RoomPlayerAvatar avatar = harness.PutRealPlayerInRoom(RoomHarness.Stranger, 2, 2);

        await harness
            .Grain.GameRuntime.Chrome.BroadcastPlayerValueAsync(RoomHarness.Stranger, 3)
            .ConfigureAwait(true);

        GamePlayerValueMessageComposer composer = harness
            .BroadcastToRoom.OfType<GamePlayerValueMessageComposer>()
            .Should()
            .ContainSingle()
            .Subject;
        composer.UserId.Should().Be(avatar.ObjectId.Value);
        composer.Value.Should().Be(3);
    }
}
