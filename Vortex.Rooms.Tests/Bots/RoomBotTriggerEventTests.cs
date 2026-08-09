using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events.Bots;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Bots;

/// <summary>
/// The two bot triggers ride room events the bots never published — both were declared against
/// AvatarWalkOnFurniEvent, which no bot raises, so neither could ever fire. These cover the events
/// themselves: whether the trigger stack in front of them matches is the wired engine's business.
/// </summary>
public sealed class RoomBotTriggerEventTests
{
    private const int BotId = 7;
    private const int EnoughTicksToArrive = 60;

    private static readonly PlayerId Bystander = new(202);

    [Fact]
    public async Task AWalkingBot_AnnouncesEveryTileItArrivesOn()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        await harness
            .BotSystem.WalkToAsync(BotId, 8, 8, CancellationToken.None)
            .ConfigureAwait(true);

        harness.RoomEvents.Clear();

        await harness.TickAsync(EnoughTicksToArrive).ConfigureAwait(true);

        BotReachedTileEvent[] arrivals = [.. harness.RoomEvents.OfType<BotReachedTileEvent>()];

        arrivals.Should().NotBeEmpty();
        arrivals
            .Should()
            .OnlyContain(
                evt => evt.BotName == RoomHarness.BotName && evt.BotId == BotId,
                "the trigger matches on the name typed into its form"
            );

        arrivals
            .Last()
            .TileIdx.Should()
            .Be(
                harness.Grain.MapModule.ToIdx(8, 8),
                "the last thing a bot sent somewhere announces is arriving there"
            );

        arrivals.Last().ObjectId.Should().Be(RoomBotSystem.ToRoomObjectId(BotId));
    }

    [Fact]
    public async Task ABotThatIsNotWalking_AnnouncesNothing()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.RoomEvents.Clear();

        await harness.TickAsync(EnoughTicksToArrive).ConfigureAwait(true);

        harness
            .RoomEvents.Should()
            .BeEmpty("a bot standing still has not reached anything it was not already at");
    }

    [Fact]
    public async Task ABotArrivingBesideSomebody_AnnouncesWhoItReached()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        harness.PutPlayerInRoom(Bystander, 8, 8);

        await harness
            .BotSystem.SetFollowTargetAsync(BotId, Bystander, CancellationToken.None)
            .ConfigureAwait(true);

        harness.RoomEvents.Clear();

        await harness.TickAsync(EnoughTicksToArrive).ConfigureAwait(true);

        BotReachedAvatarEvent met = harness
            .RoomEvents.OfType<BotReachedAvatarEvent>()
            .Should()
            .NotBeEmpty()
            .And.Subject.Last();

        met.ReachedPlayerId.Should().Be(Bystander);
        met.BotName.Should().Be(RoomHarness.BotName);
        met.CausedBy.PlayerId.Should().Be(Bystander, "the stack goes on to act on who was reached");
    }

    [Fact]
    public async Task ABotWalkingNowhereNearAnybody_AnnouncesNoMeeting()
    {
        RoomHarness harness = await RoomHarness.CreateAsync().ConfigureAwait(true);

        // The far corner from the bot's own tile, so it never passes anybody on the way.
        harness.PutPlayerInRoom(Bystander, 11, 11);

        await harness
            .BotSystem.WalkToAsync(BotId, 0, 0, CancellationToken.None)
            .ConfigureAwait(true);

        harness.RoomEvents.Clear();

        await harness.TickAsync(EnoughTicksToArrive).ConfigureAwait(true);

        harness.RoomEvents.OfType<BotReachedAvatarEvent>().Should().BeEmpty();
    }
}
