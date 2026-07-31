using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Catalog.Grains;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// The LTD raffle moves credits: entrants pay, one wins the item, the rest are refunded. Every test
/// here is about the money and the item surviving a failure or a restart — a lost refund, a
/// double-granted serial or a batch the grain forgets while its rows sit "pending" in the database
/// are all silent, permanent losses for a player.
/// </summary>
public sealed class LtdRaffleGrainTests
{
    private const int SERIES_ID = 7;
    private const int PRODUCT_ID = 70;
    private const int DEFINITION_ID = 700;
    private const int COST = 25;
    private const int WINDOW_SECONDS = 30;

    // ── 1 / 2 / 11 / 12 — the happy path, and what it must leave behind ──────

    [Fact]
    public async Task ANormalBatch_AwardsExactlyOneItemAndRefundsEveryLoser()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Inventory.Grants.Should().HaveCount(1, "exactly one LTD is awarded per draw");

        List<LtdRaffleEntryEntity> entries = await harness.EntriesAsync();

        entries.Count(e => e.Result == "won").Should().Be(1);
        entries.Count(e => e.Result == "refunded").Should().Be(2);

        // Every loser got their credits back, and only the losers.
        harness.Wallet.CreditedAmounts.Should().Equal(COST, COST);

        int winnerId = entries.Single(e => e.Result == "won").PlayerEntityId;
        harness.Wallet.CreditedPlayers.Should().NotContain(winnerId);
    }

    [Fact]
    public async Task AfterADraw_NoEntryIsLeftPending()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        List<LtdRaffleEntryEntity> entries = await harness.EntriesAsync();

        entries.Should().NotContain(e => e.Result == "pending");
        entries.Should().NotContain(e => e.Result == "processing");
    }

    [Fact]
    public async Task TheRemainingStock_IsDecrementedExactlyOnce()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        // A second call must find nothing pending and change nothing.
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        LtdSeriesEntity series = await harness.SeriesAsync();

        series.RemainingQuantity.Should().Be(4);
    }

    [Fact]
    public async Task TheWinnersSerialNumber_IsReservedWithTheStockDecrement()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        LtdRaffleEntryEntity winner = (await harness.EntriesAsync()).Single(e => e.Result == "won");

        winner.SerialNumber.Should().Be(1, "the first item of a 5-item series is serial 1");
        harness.Inventory.Grants.Single().SerialNumber.Should().Be(1);
    }

    // ── 8 — two draws must not produce two items ─────────────────────────────

    [Fact]
    public async Task RunningTheDrawTwice_AwardsOnlyOneItem()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);

        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Inventory.Grants.Should().HaveCount(1);
        (await harness.EntriesAsync()).Count(e => e.Result == "won").Should().Be(1);
    }

    // ── 5 — a refund must never be applied twice ─────────────────────────────

    [Fact]
    public async Task RunningTheDrawTwice_RefundsEachLoserOnlyOnce()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);

        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Wallet.CreditedAmounts.Should().HaveCount(2, "there are exactly two losers");
    }

    // ── 3 / 4 — a failed refund blocks finalisation and stays retryable ──────

    [Fact]
    public async Task AFailedRefund_DoesNotFinaliseTheBatchAndIsRecorded()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);

        harness.Wallet.FailCredits = true;

        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        List<LtdRaffleEntryEntity> entries = await harness.EntriesAsync();

        entries
            .Count(e => e.Result == "refund_failed")
            .Should()
            .Be(2, "a wallet failure must be visible, not swallowed");

        // The item still went out and the stock still moved — those were claimed before the refunds.
        entries.Count(e => e.Result == "won").Should().Be(1);
    }

    [Fact]
    public async Task AFailedRefund_IsRetriedOnTheNextActivation()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);

        harness.Wallet.FailCredits = true;
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Wallet.CreditedAmounts.Should().BeEmpty();

        // Restart: the wallet is healthy again.
        harness.Wallet.FailCredits = false;
        LtdRaffleGrain restarted = harness.Restart();
        await restarted.OnActivateAsync(CancellationToken.None);

        harness.Wallet.CreditedAmounts.Should().Equal(COST, COST);
        (await harness.EntriesAsync()).Count(e => e.Result == "refunded").Should().Be(2);
    }

    [Fact]
    public async Task ARetriedRefund_IsNotAppliedASecondTimeOnAFurtherRestart()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);

        harness.Wallet.FailCredits = true;
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Wallet.FailCredits = false;
        await harness.Restart().OnActivateAsync(CancellationToken.None);

        // A third activation must find nothing left to pay.
        await harness.Restart().OnActivateAsync(CancellationToken.None);

        harness.Wallet.CreditedAmounts.Should().HaveCount(2);
    }

    // ── 10 — a failure after the grant must not allow a second grant ─────────

    [Fact]
    public async Task AFailedGrant_IsRecordedAndNeverRetried()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2);

        harness.Inventory.FailGrants = true;
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        (await harness.EntriesAsync()).Count(e => e.Result == "grant_failed").Should().Be(1);

        // The serial and the stock are already consumed, so re-running must not hand out a second one.
        harness.Inventory.FailGrants = false;
        await harness.Restart().OnActivateAsync(CancellationToken.None);
        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Inventory.Grants.Should().BeEmpty("no grant ever succeeded, and none is replayed");
        (await harness.SeriesAsync()).RemainingQuantity.Should().Be(4);
    }

    // ── 6 / 7 — restart recovery ─────────────────────────────────────────────

    [Fact]
    public async Task AReactivatedGrain_RearmsTheDrawForAnUnexpiredBatch()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2);

        LtdRaffleGrain restarted = harness.Restart();
        await restarted.OnActivateAsync(CancellationToken.None);

        restarted
            .NextDrawDueUtc.Should()
            .NotBeNull("a restart must not leave the batch with no scheduled draw");

        // Rebuilt from the first entry's persisted timestamp plus the raffle window.
        restarted
            .NextDrawDueUtc!.Value.Should()
            .BeCloseTo(DateTime.UtcNow.AddSeconds(WINDOW_SECONDS), TimeSpan.FromSeconds(5));

        harness.Inventory.Grants.Should().BeEmpty("the window has not elapsed yet");
    }

    [Fact]
    public async Task AReactivatedGrain_RunsTheDrawImmediatelyForAnExpiredBatch()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2, 3);

        // The process was down longer than the raffle window.
        await harness.BackdateEntriesAsync(TimeSpan.FromSeconds(WINDOW_SECONDS + 60));

        LtdRaffleGrain restarted = harness.Restart();
        await restarted.OnActivateAsync(CancellationToken.None);

        harness.Inventory.Grants.Should().HaveCount(1, "an overdue batch is drawn on activation");
        restarted.NextDrawDueUtc.Should().BeNull();
        (await harness.EntriesAsync()).Should().NotContain(e => e.Result == "pending");
    }

    // ── 9 — batches must never be mixed ──────────────────────────────────────

    [Fact]
    public async Task EntriesFromDifferentBatches_AreNeverDrawnTogether()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        DateTime oldBatchEntered = DateTime.UtcNow.AddSeconds(-(WINDOW_SECONDS + 60));

        await harness.SeedEntriesAsync("batch-old", oldBatchEntered, 1, 2);
        await harness.SeedEntriesAsync("batch-new", DateTime.UtcNow, 3, 4);

        LtdRaffleGrain restarted = harness.Restart();
        await restarted.OnActivateAsync(CancellationToken.None);

        List<LtdRaffleEntryEntity> entries = await harness.EntriesAsync();

        // Only the older batch was drawn.
        entries
            .Where(e => e.BatchId == "batch-old")
            .Should()
            .OnlyContain(e => e.Result == "won" || e.Result == "refunded");

        entries
            .Where(e => e.BatchId == "batch-new")
            .Should()
            .OnlyContain(e => e.Result == "pending", "the newer batch keeps its own window");

        harness.Inventory.Grants.Should().HaveCount(1);
    }

    // ── Exhausted / unawardable stock must still return the money ────────────

    [Fact]
    public async Task ABatchThatCannotBeAwarded_RefundsEveryEntrant()
    {
        Harness harness = await Harness.CreateAsync(totalQuantity: 5, remainingQuantity: 5);

        await harness.EnterAsync(1, 2);

        // The last item went to another series activation between the entries and the draw.
        await harness.SetRemainingQuantityAsync(0);
        await harness.Grain.ReloadSeriesAsync(CancellationToken.None);

        await harness.Grain.ForceRunRaffleAsync(CancellationToken.None);

        harness.Inventory.Grants.Should().BeEmpty();
        harness.Wallet.CreditedAmounts.Should().Equal(COST, COST);
        (await harness.EntriesAsync()).Should().OnlyContain(e => e.Result == "refunded");
    }

    // ── Harness ──────────────────────────────────────────────────────────────

    private sealed class Harness
    {
        private readonly DbContextOptions<VortexDbContext> _options;
        private int _nextEntryPlayerSeed;

        private Harness(DbContextOptions<VortexDbContext> options)
        {
            _options = options;
        }

        public LtdRaffleGrain Grain { get; private set; } = null!;

        public RecordingWallet Wallet { get; } = new();

        public RecordingInventory Inventory { get; } = new();

        public static async Task<Harness> CreateAsync(int totalQuantity, int remainingQuantity)
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"ltd-raffle-{Guid.NewGuid():N}")
                    .Options;

            Harness harness = new(options);

            await using (VortexDbContext dbCtx = new(options))
            {
                // The grain only reads the product's furniture-definition id; the definition row
                // itself belongs to the inventory grain, which is faked here.
                dbCtx.CatalogProducts.Add(
                    new CatalogProductEntity
                    {
                        Id = PRODUCT_ID,
                        CatalogOfferEntityId = 1,
                        FurnitureDefinitionEntityId = DEFINITION_ID,
                        ProductType = ProductType.Floor,
                        Quantity = 1,
                        UniqueSize = 0,
                        UniqueRemaining = 0,
                        BuildersClubEligible = false,
                        Offer = null!,
                    }
                );

                dbCtx.LtdSeries.Add(
                    new LtdSeriesEntity
                    {
                        Id = SERIES_ID,
                        CatalogProductEntityId = PRODUCT_ID,
                        TotalQuantity = totalQuantity,
                        RemainingQuantity = remainingQuantity,
                        CostCredits = COST,
                        RaffleWindowSeconds = WINDOW_SECONDS,
                        IsActive = true,
                        HasRaffleFinished = false,
                        CatalogProductEntity = null!,
                    }
                );

                for (int playerId = 1; playerId <= 8; playerId++)
                {
                    dbCtx.Players.Add(
                        new PlayerEntity
                        {
                            Id = playerId,
                            Name = $"player{playerId}",
                            Figure = string.Empty,
                            Gender = AvatarGenderType.Male,
                            PlayerStatus = PlayerStatusType.Offline,
                            PlayerPerks = PlayerPerkFlags.None,
                        }
                    );
                }

                await dbCtx.SaveChangesAsync();
            }

            harness.Grain = harness.Build();
            await harness.Grain.OnActivateAsync(CancellationToken.None);

            return harness;
        }

        /// <summary>Simulates a process restart: a brand-new activation over the same database.</summary>
        public LtdRaffleGrain Restart()
        {
            Grain = Build();

            return Grain;
        }

        public async Task EnterAsync(params int[] playerIds)
        {
            foreach (int playerId in playerIds)
            {
                LtdRaffleEntryResult result = await Grain.EnterRaffleAsync(
                    PlayerId.Parse(playerId),
                    CancellationToken.None
                );

                result.Success.Should().BeTrue($"player {playerId} should be able to enter");
            }
        }

        /// <summary>Writes entries directly so a specific batch id and age can be set up.</summary>
        public async Task SeedEntriesAsync(string batchId, DateTime enteredAt, params int[] players)
        {
            await using VortexDbContext dbCtx = new(_options);

            foreach (int playerId in players)
            {
                dbCtx.LtdRaffleEntries.Add(
                    new LtdRaffleEntryEntity
                    {
                        Id = ++_nextEntryPlayerSeed + 1000,
                        SeriesEntityId = SERIES_ID,
                        PlayerEntityId = playerId,
                        BatchId = batchId,
                        EnteredAt = enteredAt,
                        Result = "pending",
                        SeriesEntity = null!,
                        PlayerEntity = null!,
                    }
                );
            }

            await dbCtx.SaveChangesAsync();
        }

        public async Task BackdateEntriesAsync(TimeSpan age)
        {
            await using VortexDbContext dbCtx = new(_options);

            List<LtdRaffleEntryEntity> entries = await dbCtx.LtdRaffleEntries.ToListAsync();

            foreach (LtdRaffleEntryEntity entry in entries)
            {
                entry.EnteredAt = DateTime.UtcNow - age;
            }

            await dbCtx.SaveChangesAsync();
        }

        public async Task SetRemainingQuantityAsync(int remaining)
        {
            await using VortexDbContext dbCtx = new(_options);

            LtdSeriesEntity series = await dbCtx.LtdSeries.SingleAsync(s => s.Id == SERIES_ID);
            series.RemainingQuantity = remaining;

            await dbCtx.SaveChangesAsync();
        }

        public async Task<List<LtdRaffleEntryEntity>> EntriesAsync()
        {
            await using VortexDbContext dbCtx = new(_options);

            return await dbCtx.LtdRaffleEntries.AsNoTracking().ToListAsync();
        }

        public async Task<LtdSeriesEntity> SeriesAsync()
        {
            await using VortexDbContext dbCtx = new(_options);

            return await dbCtx.LtdSeries.AsNoTracking().SingleAsync(s => s.Id == SERIES_ID);
        }

        private LtdRaffleGrain Build() =>
            GrainActivationContext.CreateWithIntegerKey<LtdRaffleGrain>(
                SERIES_ID,
                new TestDbContextFactory(_options),
                BuildGrainFactory(),
                FakeProxy.Create<IEventPublisher>(_ => null),
                NullLogger<LtdRaffleGrain>.Instance
            );

        private IGrainFactory BuildGrainFactory() =>
            FakeProxy.Create<IGrainFactory>(call =>
            {
                if (!call.Method.IsGenericMethod)
                {
                    return null;
                }

                Type target = call.Method.GetGenericArguments()[0];

                if (target == typeof(IPlayerWalletGrain))
                {
                    return Wallet.AsGrain((int)(long)call.Args![0]!);
                }

                return target == typeof(IInventoryGrain)
                    ? Inventory.AsGrain((int)(long)call.Args![0]!)
                    : null;
            });
    }

    /// <summary>Records what was given back, and to whom — the numbers these tests are about.</summary>
    private sealed class RecordingWallet
    {
        public bool FailCredits { get; set; }

        public List<int> CreditedAmounts { get; } = [];

        public List<int> CreditedPlayers { get; } = [];

        public IPlayerWalletGrain AsGrain(int playerId) =>
            FakeProxy.Create<IPlayerWalletGrain>(call =>
            {
                switch (call.Method.Name)
                {
                    case nameof(IPlayerWalletGrain.TryDebitAsync):
                        return Task.FromResult(WalletDebitResult.Success());

                    case nameof(IPlayerWalletGrain.GrantCreditsAsync):
                        if (FailCredits)
                        {
                            return Task.FromException(
                                new InvalidOperationException("wallet unavailable")
                            );
                        }

                        CreditedAmounts.Add((int)call.Args![0]!);
                        CreditedPlayers.Add(playerId);

                        return Task.CompletedTask;

                    default:
                        return null;
                }
            });
    }

    private sealed record LtdGrant(
        int PlayerId,
        int DefinitionId,
        int SerialNumber,
        int SeriesSize
    );

    private sealed class RecordingInventory
    {
        public bool FailGrants { get; set; }

        public List<LtdGrant> Grants { get; } = [];

        public IInventoryGrain AsGrain(int playerId) =>
            FakeProxy.Create<IInventoryGrain>(call =>
            {
                if (call.Method.Name != nameof(IInventoryGrain.GrantLtdFurnitureAsync))
                {
                    return null;
                }

                if (FailGrants)
                {
                    return Task.FromException(
                        new InvalidOperationException("inventory unavailable")
                    );
                }

                Grants.Add(
                    new LtdGrant(
                        playerId,
                        (int)call.Args![0]!,
                        (int)call.Args[1]!,
                        (int)call.Args[2]!
                    )
                );

                return Task.CompletedTask;
            });
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
