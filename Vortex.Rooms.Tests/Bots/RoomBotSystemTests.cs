using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Orleans.Snapshots.Room.Settings;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Primitives.Rooms.Snapshots;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Wired.Logs;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Bots;

/// <summary>
/// The decisions RoomBotSystem makes that a wire test cannot see: where a picked-up bot goes, who
/// is allowed to pick it up, and that a bot's room object id cannot collide with a pet's. All three
/// are the sort of thing that looks fine until two players and a pet are in the same room.
/// </summary>
public sealed class RoomBotSystemTests
{
    private const int ROOM_ID = 55;
    private static readonly PlayerId Owner = new(101);
    private static readonly PlayerId Stranger = new(202);

    [Fact]
    public void BotObjectIds_CannotCollideWithPetObjectIds()
    {
        // Pets sit at +1,000,000. A bot id low enough to land in that range would have the client
        // drawing one occupant over the other.
        RoomObjectId bot = RoomBotSystem.ToRoomObjectId(1);

        bot.Value.Should().BeGreaterThan(1_000_000 + 999_999);
    }

    [Fact]
    public async Task PlacedBots_BecomeAvatarsCarryingTheirOwnerAndObjectId()
    {
        Harness harness = await Harness.CreateAsync(placedBotId: 7).ConfigureAwait(true);

        ImmutableArray<RoomAvatarSnapshot> avatars = await harness
            .Grain.GetPlacedBotAvatarSnapshotsAsync(CancellationToken.None)
            .ConfigureAwait(true);

        RoomBotAvatarSnapshot bot = avatars
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<RoomBotAvatarSnapshot>()
            .Subject;

        bot.WebId.Should().Be(7);
        bot.OwnerId.Should().Be(Owner.Value);
        bot.AvatarType.Should().Be(RoomObjectType.Bot);
        bot.ObjectId.Should().Be(RoomBotSystem.ToRoomObjectId(7));
    }

    [Fact]
    public async Task RemovingABotThatIsNotHere_DoesNothing()
    {
        Harness harness = await Harness.CreateAsync(placedBotId: 7).ConfigureAwait(true);

        bool removed = await harness
            .Grain.RemoveBotAsync(harness.ContextFor(Owner), 999, CancellationToken.None)
            .ConfigureAwait(true);

        removed.Should().BeFalse();
        harness.ComposersSentTo.Should().BeEmpty("nothing happened, so nobody should be told");
    }

    [Fact]
    public async Task RemovingABot_ReturnsItToItsOwnersHandNotTheRemovers()
    {
        // The room owner clearing up after a visitor must not end up owning the visitor's bot.
        Harness harness = await Harness
            .CreateAsync(placedBotId: 7, canManipulate: true)
            .ConfigureAwait(true);

        bool removed = await harness
            .Grain.RemoveBotAsync(harness.ContextFor(Stranger), 7, CancellationToken.None)
            .ConfigureAwait(true);

        removed.Should().BeTrue();
        harness
            .ComposersSentTo.Should()
            .Contain(Owner, "the bot goes back to the hand it came from");
        harness.ComposersSentTo.Should().NotContain(Stranger);
    }

    [Fact]
    public async Task RemovingABot_ClearsItsRoomSoTheOwnerSeesItInTheirInventoryAgain()
    {
        Harness harness = await Harness.CreateAsync(placedBotId: 7).ConfigureAwait(true);

        await harness
            .Grain.RemoveBotAsync(harness.ContextFor(Owner), 7, CancellationToken.None)
            .ConfigureAwait(true);

        await using VortexDbContext dbCtx = harness.NewDbContext();

        BotEntity bot = await dbCtx.Bots.SingleAsync(b => b.Id == 7).ConfigureAwait(true);

        bot.RoomEntityId.Should().BeNull("a picked-up bot is back in the hand, not in the room");
    }

    [Fact]
    public async Task AStrangerWithoutRights_CannotPickUpSomebodyElsesBot()
    {
        Harness harness = await Harness
            .CreateAsync(placedBotId: 7, canManipulate: false)
            .ConfigureAwait(true);

        bool removed = await harness
            .Grain.RemoveBotAsync(harness.ContextFor(Stranger), 7, CancellationToken.None)
            .ConfigureAwait(true);

        removed.Should().BeFalse();

        await using VortexDbContext dbCtx = harness.NewDbContext();

        BotEntity bot = await dbCtx.Bots.SingleAsync(b => b.Id == 7).ConfigureAwait(true);

        bot.RoomEntityId.Should().Be(ROOM_ID, "the bot must still be standing where it was");
    }

    private sealed class Harness
    {
        private readonly DbContextOptions<VortexDbContext> _options;

        private Harness(DbContextOptions<VortexDbContext> options, bool canManipulate)
        {
            _options = options;

            Grain = GrainActivationContext.CreateWithIntegerKey<RoomGrain>(
                ROOM_ID,
                new SingleOptionsFactory(options),
                Options.Create(new RoomConfig()),
                NullLogger<IRoomGrain>.Instance,
                FakeProxy.Create<IRoomModelProvider>(_ => null),
                FakeProxy.Create<IRoomItemsProvider>(_ => null),
                FakeProxy.Create<IRoomObjectLogicProvider>(_ => null),
                FakeProxy.Create<IRoomAvatarProvider>(_ => null),
                FakeProxy.Create<IRoomWiredVariablesProvider>(_ => null),
                BuildGrainFactory(),
                FakeProxy.Create<IEventPublisher>(_ => null),
                BuildPermissionService(),
                FakeProxy.Create<IVortexMetrics>(_ => null),
                FakeProxy.Create<IRoomModerationStore>(_ => null),
                FakeProxy.Create<IPetLevelProvider>(_ => null),
                FakeProxy.Create<IPetCommandProvider>(_ => null),
                FakeProxy.Create<IPetVocalProvider>(_ => null),
                new RoomWiredLogChannel()
            );

            // The security module reads the room's own snapshot to decide who owns the place, so a
            // grain built outside a silo needs one before any rights question can be asked.
            Grain._state.RoomSnapshot = new RoomSnapshot
            {
                RoomId = new RoomId(ROOM_ID),
                Name = "Test room",
                Description = string.Empty,
                OwnerId = Owner,
                OwnerName = "Owner",
                Population = 0,
                LastUpdatedUtc = DateTime.UnixEpoch,
                DoorMode = RoomDoorModeType.Open,
                PlayersMax = 25,
                TradeType = RoomTradeModeType.Disabled,
                Score = 0,
                Ranking = 0,
                CategoryId = -1,
                Tags = [],
                AllowBlocking = false,
                AllowPets = true,
                AllowPetsEat = false,
                PaintWall = 0,
                PaintFloor = 0,
                PaintLandscape = 0,
                StaffPick = false,
                Password = string.Empty,
                ModSettings = new ModSettingsSnapshot
                {
                    WhoCanMute = ModSettingType.Owner,
                    WhoCanKick = ModSettingType.Owner,
                    WhoCanBan = ModSettingType.Owner,
                },
                ChatSettings = new ChatSettingsSnapshot
                {
                    ChatMode = ChatModeType.FreeFlow,
                    BubbleWidth = ChatBubbleWidthType.Normal,
                    ScrollSpeed = ChatScrollSpeedType.Normal,
                    FullHearRange = 50,
                    FloodSensitivity = ChatFloodSensitivityType.Normal,
                },
                WorldType = string.Empty,
                HideWalls = false,
                WallThickness = RoomThicknessType.Normal,
                FloorThickness = RoomThicknessType.Normal,
                MaxVisitorsLimit = 25,
            };

            if (canManipulate)
            {
                // Build rights in this room, which is what lets somebody clear up furniture — and
                // now bots — that is not theirs.
                Grain._state.PlayerIdsWithRights.Add(Stranger);
            }

            // The room broadcast normally rides an Orleans stream, which does not exist outside a
            // silo. Swapping it for a recorder keeps the code under test unchanged and makes what
            // the room was told an assertable thing rather than a crash.
            Grain._roomOutbound = FakeProxy.Create<IAsyncStream<RoomOutbound>>(call =>
            {
                if (call.Method.Name == nameof(IAsyncStream<RoomOutbound>.OnNextAsync))
                {
                    BroadcastToRoom.Add(((RoomOutbound)call.Args![0]!).Composer);
                }

                return null;
            });
        }

        public RoomGrain Grain { get; }

        /// <summary>Who the grain pushed an inventory composer at — the point of the ownership test.</summary>
        public List<PlayerId> ComposersSentTo { get; } = [];

        /// <summary>What the whole room was told, in order.</summary>
        public List<IComposer> BroadcastToRoom { get; } = [];

        public static async Task<Harness> CreateAsync(int placedBotId, bool canManipulate = true)
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"room-bots-{Guid.NewGuid():N}")
                    .Options;

            await using (VortexDbContext seed = new(options))
            {
                seed.Bots.Add(
                    new BotEntity
                    {
                        Id = placedBotId,
                        OwnerPlayerEntityId = Owner.Value,
                        RoomEntityId = ROOM_ID,
                        Name = "Bartender",
                        Figure = "hd-180-1",
                        Gender = AvatarGenderType.Male,
                        X = 3,
                        Y = 4,
                    }
                );

                await seed.SaveChangesAsync().ConfigureAwait(true);
            }

            return new Harness(options, canManipulate);
        }

        public VortexDbContext NewDbContext() => new(_options);

        public ActionContext ContextFor(PlayerId playerId) =>
            new(ActionOrigin.Player, default, playerId, new RoomId(ROOM_ID));

        /// <summary>
        /// A staff-capability-free player, so the manipulate check falls through to the room's own
        /// rights list — which is what the tests vary.
        /// </summary>
        private static IPermissionService BuildPermissionService() =>
            FakeProxy.Create<IPermissionService>(call =>
                call.Method.Name == nameof(IPermissionService.ResolveForPlayerAsync)
                    ? Task.FromResult(new PermissionSet([], []))
                    : null
            );

        private IGrainFactory BuildGrainFactory() =>
            FakeProxy.Create<IGrainFactory>(call =>
            {
                if (
                    !call.Method.IsGenericMethod
                    || call.Method.GetGenericArguments()[0] != typeof(IPlayerPresenceGrain)
                )
                {
                    return null;
                }

                PlayerId target = new((int)(long)call.Args![0]!);

                return FakeProxy.Create<IPlayerPresenceGrain>(inner =>
                {
                    if (inner.Method.Name == nameof(IPlayerPresenceGrain.SendComposerAsync))
                    {
                        ComposersSentTo.Add(target);
                    }

                    return null;
                });
            });

        private sealed class SingleOptionsFactory(DbContextOptions<VortexDbContext> options)
            : IDbContextFactory<VortexDbContext>
        {
            public VortexDbContext CreateDbContext() => new(options);
        }
    }
}
