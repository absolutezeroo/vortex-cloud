using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Wired;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Grains.Systems.WiredTrading;
using Vortex.Rooms.Wired.Logs;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The lifecycle of a trade screen, which used to be two dictionaries that had to agree.
/// </summary>
/// <remarks>
/// A contract offer and the screen it opens are one record now. Each case here is a way they could
/// come apart when they were two: a player who walks out, a client that ignores its own timer, and
/// a box that offers again.
/// </remarks>
public sealed class WiredTradeSessionTests
{
    private const int ROOM_ID = 55;
    private const int CHEST_ID = 900;
    private const int CONTRACT_ID = 901;
    private const int OTHER_CONTRACT_ID = 902;
    private const int PLAYER_ID = 11;

    private static readonly PlayerId Player = new(PLAYER_ID);

    [Fact]
    public async Task APlayerWhoWalksOutMidOffer_FailsTheTransaction()
    {
        Harness harness = new();
        await harness.OfferAsync().ConfigureAwait(true);

        await harness.Grain.CloseChestScreensForLeavingPlayerAsync(Player).ConfigureAwait(true);

        // The cleanup used to forget the screen and leave the offer pending for the life of the
        // room, so a stack listening on wf_trg_transaction_failed never heard about a leaver.
        harness.Sessions.Should().BeEmpty();
        harness.Failed.Should().ContainSingle();
    }

    [Fact]
    public async Task APlainDepositWalkedOutOn_FailsNothing()
    {
        Harness harness = new();

        await harness
            .Grain.StartWiredChestDepositAsync(harness.Context, CHEST_ID, CancellationToken.None)
            .ConfigureAwait(true);

        harness.Sessions.Should().ContainKey(Player);

        await harness.Grain.CloseChestScreensForLeavingPlayerAsync(Player).ConfigureAwait(true);

        // A donation is not a transaction, and raising the failure trigger for one would fire wiring
        // that was never offered anything.
        harness.Sessions.Should().BeEmpty();
        harness.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task AnOfferPastItsDeadline_CannotBeAccepted()
    {
        Harness harness = new();
        await harness.OfferAsync(timeoutSeconds: 30).ConfigureAwait(true);

        harness.ExpireNow();

        await harness
            .Grain.AcceptWiredDepositAsync(harness.Context, confirm: true, CancellationToken.None)
            .ConfigureAwait(true);

        // The deadline used to be checked only when a box offered or cancelled, so a client that
        // ignored its own timer could still settle on a price that had run out.
        harness.Sessions.Should().BeEmpty();
        harness.Failed.Should().ContainSingle();
    }

    [Fact]
    public async Task AnOfferThatReplacesAnother_FailsTheOneItWithdrew()
    {
        Harness harness = new();
        await harness.OfferAsync().ConfigureAwait(true);

        await harness.OfferAsync(contractId: OTHER_CONTRACT_ID).ConfigureAwait(true);

        harness.Failed.Should().ContainSingle();
        harness.Sessions[Player].ContractId.Should().Be(OTHER_CONTRACT_ID);
    }

    [Fact]
    public async Task AnOfferWithNoTerms_LeavesTheScreenAlone()
    {
        Harness harness = new();
        await harness.OfferAsync().ConfigureAwait(true);

        // No stored contract, and no add-on terms passed either.
        bool offered = await harness
            .Grain.OfferTransactionAsync(
                OTHER_CONTRACT_ID,
                Player,
                CHEST_ID,
                contract: null,
                mode: 0,
                multiplier: 1,
                timeoutSeconds: 0,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        offered.Should().BeFalse();

        // An offer that is never made withdraws nothing: the standing one is still on screen.
        harness.Sessions[Player].ContractId.Should().Be(CONTRACT_ID);
        harness.Failed.Should().BeEmpty();
    }

    private sealed class Harness
    {
        public Harness()
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"wired-session-{Guid.NewGuid()}")
                    .Options;

            // Open to donations, so the deposit under test is gated by the session logic rather
            // than by the rights of a room whose snapshot these tests never stand up.
            using (VortexDbContext db = new(options))
            {
                db.WiredChests.Add(
                    new WiredChestEntity
                    {
                        FurnitureEntityId = CHEST_ID,
                        Credits = 0,
                        NotificationsEnabled = false,
                        EveryoneCanDonate = true,
                    }
                );

                db.SaveChanges();
            }

            Grain = GrainActivationContext.CreateWithIntegerKey<RoomGrain>(
                ROOM_ID,
                new TestDbContextFactory(options),
                FakeProxy.Create<IFurnitureDefinitionProvider>(_ => null),
                new StuffDataFactory(),
                Options.Create(new RoomConfig()),
                NullLogger<IRoomGrain>.Instance,
                FakeProxy.Create<IRoomModelProvider>(_ => null),
                FakeProxy.Create<IRoomItemsProvider>(_ => null),
                FakeProxy.Create<IRoomObjectLogicProvider>(_ => null),
                FakeProxy.Create<IRoomAvatarProvider>(_ => null),
                FakeProxy.Create<IRoomWiredVariablesProvider>(_ => null),
                FakeProxy.Create<IGrainFactory>(call =>
                    call.Method.IsGenericMethod
                    && call.Method.GetGenericArguments()[0] == typeof(IPlayerPresenceGrain)
                        ? FakeProxy.Create<IPlayerPresenceGrain>(_ => Task.CompletedTask)
                        : null
                ),
                FakeProxy.Create<IEventPublisher>(_ => null),
                FakeProxy.Create<IPermissionService>(_ => null),
                FakeProxy.Create<IVortexMetrics>(_ => null),
                FakeProxy.Create<IRoomModerationStore>(_ => null),
                FakeProxy.Create<IPetLevelProvider>(_ => null),
                FakeProxy.Create<IPetCommandProvider>(_ => null),
                FakeProxy.Create<IPetVocalProvider>(_ => null),
                new RoomWiredLogChannel()
            );

            Grain._state.ItemsById[CHEST_ID] = ChestItem();
            Grain._state.ItemsById[CONTRACT_ID] = ChestItem();
            Grain._state.ItemsById[OTHER_CONTRACT_ID] = ChestItem();

            // Wired triggers read room events, so this is where a test sees one fire without
            // standing the whole wired engine up.
            Grain.EventModule.Register(new EventRecorder(Events));
        }

        public RoomGrain Grain { get; }

        public List<RoomEvent> Events { get; } = [];

        public IEnumerable<RoomEvent> Failed => Events.OfType<WiredTransactionFailedEvent>();

        public IReadOnlyDictionary<PlayerId, WiredTradeSession> Sessions =>
            Grain.WiredTradingSystem._sessions;

        public ActionContext Context =>
            new(ActionOrigin.Player, default, Player, new RoomId(ROOM_ID));

        public Task<bool> OfferAsync(int contractId = CONTRACT_ID, int timeoutSeconds = 0) =>
            Grain.OfferTransactionAsync(
                contractId,
                Player,
                CHEST_ID,
                new TradeContract
                {
                    YouGiveRules = [Rule(10)],
                    YouGetRule = Rule(5),
                    Mode = 0,
                    Multiplier = 1,
                    AutoMultiplierMax = 1,
                },
                mode: 0,
                multiplier: 1,
                timeoutSeconds,
                CancellationToken.None
            );

        /// <summary>Puts the standing offer's deadline in the past, which is the state a real one
        /// reaches by waiting — and waiting is not something a test should do.</summary>
        public void ExpireNow() =>
            Grain.WiredTradingSystem._sessions[Player] = Grain.WiredTradingSystem._sessions[
                Player
            ] with
            {
                ExpiresAt = DateTime.UtcNow.AddSeconds(-1),
            };

        private static TradeContractRule Rule(int coins) =>
            new() { Nodes = [new TradeContractNode { IsFurni = false, Amount = coins }] };

        private static IRoomItem ChestItem()
        {
            FurnitureDefinitionSnapshot definition = new()
            {
                Id = 1,
                SpriteId = 1,
                Name = "chest",
                ProductType = ProductType.Floor,
                FurniCategory = FurnitureCategory.Default,
                LogicName = "furniture_furnichest",
                TotalStates = 1,
                Width = 1,
                Length = 1,
                StackHeight = Altitude.Zero,
                CanStack = false,
                CanWalk = false,
                CanSit = false,
                CanLay = false,
                CanRecycle = false,
                CanTrade = false,
                CanGroup = false,
                CanSell = false,
                UsagePolicy = FurnitureUsageType.Nobody,
                ExtraData = null,
                StuffDataType = StuffDataType.LegacyKey,
            };

            return FakeProxy.Create<IRoomItem>(call =>
                call.Method.Name == $"get_{nameof(IRoomItem.Definition)}" ? definition : null
            );
        }
    }

    private sealed class EventRecorder(List<RoomEvent> events) : IRoomEventListener
    {
        public Task OnRoomEventAsync(RoomEvent evt, CancellationToken ct)
        {
            events.Add(evt);

            return Task.CompletedTask;
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
