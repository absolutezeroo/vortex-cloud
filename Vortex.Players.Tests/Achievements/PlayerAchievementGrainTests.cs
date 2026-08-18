using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Achievements;
using Vortex.Players.Configuration;
using Vortex.Players.Grains;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Players.Tests.Achievements;

/// <summary>
///     Progression is one counter bump per room entry, per respect, per furni placed, so it is the
///     hottest write in the player domain. The grain holds the counters in memory and batches them,
///     with one exception: a level-up hands out a badge and currency, so its row goes through before
///     anything is granted on the back of it. These tests pin both halves of that split.
/// </summary>
public sealed class PlayerAchievementGrainTests
{
    private const int PlayerId = 42;
    private const int AchievementId = 7;
    private const string RoomEntry = "RoomEntry";

    [Fact]
    public async Task ProgressBelowALevel_IsHeldInMemoryAndWrittenOnceOnTheFlush()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        for (int i = 0; i < 3; i++)
        {
            await harness
                .Grain.ProgressAsync(RoomEntry, 1, CancellationToken.None)
                .ConfigureAwait(true);
        }

        (await harness.ReadRowAsync().ConfigureAwait(true))
            .Should()
            .BeNull("three counter bumps below the first level owe the database nothing yet");

        await harness.FlushAsync().ConfigureAwait(true);

        PlayerAchievementEntity? row = await harness.ReadRowAsync().ConfigureAwait(true);

        row.Should().NotBeNull();
        row!.Progress.Should().Be(3, "the flush writes the counter the player actually reached");
        row.Level.Should().Be(0);

        // One for the hydration read, one for the flush -- not one round-trip per event.
        harness.DbContextsCreated.Should().Be(2);
    }

    [Fact]
    public async Task TheAchievementsWindow_ReadsTheHeldCounterRatherThanTheStoredOne()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.ProgressAsync(RoomEntry, 2, CancellationToken.None)
            .ConfigureAwait(true);

        AchievementListSnapshot list = await harness
            .Grain.GetAchievementsAsync(CancellationToken.None)
            .ConfigureAwait(true);

        list.Achievements.Should()
            .ContainSingle()
            .Which.CurrentProgress.Should()
            .Be(2, "the player sees what they just did, not what the flush timer has caught up to");
    }

    [Fact]
    public async Task ALevelUp_IsWrittenThroughWithoutWaitingForTheFlush()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.ProgressAsync(RoomEntry, 5, CancellationToken.None)
            .ConfigureAwait(true);

        PlayerAchievementEntity? row = await harness.ReadRowAsync().ConfigureAwait(true);

        row.Should().NotBeNull("a badge and a reward ride on this level; the row cannot lag");
        row!.Level.Should().Be(1);
        row.Progress.Should().Be(5);
    }

    [Fact]
    public async Task AReactivatedGrain_PicksUpFromTheStoredCounter()
    {
        Harness harness = await Harness.CreateAsync(seededProgress: 4).ConfigureAwait(true);

        await harness
            .Grain.ProgressAsync(RoomEntry, 1, CancellationToken.None)
            .ConfigureAwait(true);

        PlayerAchievementEntity? row = await harness.ReadRowAsync().ConfigureAwait(true);

        row!.Level.Should().Be(1, "hydration means the fifth entry completes the level");
        row.Progress.Should().Be(5);
    }

    [Fact]
    public async Task ADailyAchievement_StillAdvancesOnlyOncePerDay()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        for (int i = 0; i < 3; i++)
        {
            await harness
                .Grain.ProgressDailyAsync(RoomEntry, 1, CancellationToken.None)
                .ConfigureAwait(true);
        }

        await harness.FlushAsync().ConfigureAwait(true);

        PlayerAchievementEntity? row = await harness.ReadRowAsync().ConfigureAwait(true);

        row!
            .Progress.Should()
            .Be(1, "the guard now reads the in-memory timestamp, not the row's lagging updated_at");
    }

    [Fact]
    public async Task ASecondFlush_WithNothingNew_LeavesTheDatabaseAlone()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.ProgressAsync(RoomEntry, 1, CancellationToken.None)
            .ConfigureAwait(true);
        await harness.FlushAsync().ConfigureAwait(true);

        int afterFirstFlush = harness.DbContextsCreated;

        await harness.FlushAsync().ConfigureAwait(true);

        harness
            .DbContextsCreated.Should()
            .Be(afterFirstFlush, "an idle player must not wake the database every tick");
    }

    private sealed class Harness
    {
        private readonly DbContextOptions<VortexDbContext> _options;

        private Harness(
            PlayerAchievementGrain grain,
            DbContextOptions<VortexDbContext> options,
            CountingDbContextFactory factory
        )
        {
            Grain = grain;
            _options = options;
            Factory = factory;
        }

        public PlayerAchievementGrain Grain { get; }

        private CountingDbContextFactory Factory { get; }

        public int DbContextsCreated => Factory.Created;

        /// <summary>Deactivation is the flush the tests can reach; the timer runs the same code.</summary>
        public Task FlushAsync() =>
            Grain.OnDeactivateAsync(
                new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "test"),
                CancellationToken.None
            );

        public async Task<PlayerAchievementEntity?> ReadRowAsync()
        {
            await using VortexDbContext dbCtx = new(_options);

            return await dbCtx
                .PlayerAchievements.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PlayerEntityId == PlayerId && p.AchievementEntityId == AchievementId
                )
                .ConfigureAwait(true);
        }

        public static async Task<Harness> CreateAsync(int seededProgress = 0, int seededLevel = 0)
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"achievements-{Guid.NewGuid():N}")
                    .Options;

            if (seededProgress > 0 || seededLevel > 0)
            {
                await using VortexDbContext seed = new(options);

                seed.PlayerAchievements.Add(
                    new PlayerAchievementEntity
                    {
                        Id = 1,
                        PlayerEntityId = PlayerId,
                        AchievementEntityId = AchievementId,
                        Progress = seededProgress,
                        Level = seededLevel,
                    }
                );

                await seed.SaveChangesAsync().ConfigureAwait(true);
            }

            CountingDbContextFactory factory = new(options);

            PlayerAchievementGrain grain =
                GrainActivationContext.CreateWithIntegerKey<PlayerAchievementGrain>(
                    PlayerId,
                    BuildGrainFactory(),
                    factory,
                    Options.Create(new AchievementConfig()),
                    NullLogger<PlayerAchievementGrain>.Instance
                );

            await grain.OnActivateAsync(CancellationToken.None).ConfigureAwait(true);

            return new Harness(grain, options, factory);
        }

        /// <summary>
        /// Only the definition cache is answered for real. Everything progression fans out to --
        /// presence, inventory, wallet, resolutions -- is a stub: what it does with a level-up is
        /// its own business, and none of it is what these tests are pinning.
        /// </summary>
        private static IGrainFactory BuildGrainFactory()
        {
            IAchievementManagerGrain manager = FakeProxy.Create<IAchievementManagerGrain>(call =>
                call.Method.Name switch
                {
                    nameof(IAchievementManagerGrain.GetDefinitionsAsync) => Task.FromResult(
                        ImmutableArray.Create(Definition())
                    ),
                    nameof(IAchievementManagerGrain.GetByNameAsync) =>
                        Task.FromResult<AchievementDefinitionSnapshot?>(
                            string.Equals(
                                call.Args?[0] as string,
                                RoomEntry,
                                StringComparison.Ordinal
                            )
                                ? Definition()
                                : null
                        ),
                    nameof(IAchievementManagerGrain.GetDefaultCategoryAsync) => Task.FromResult(
                        "explore"
                    ),
                    _ => null,
                }
            );

            return FakeProxy.Create<IGrainFactory>(call =>
                call.Method.IsGenericMethod
                    ? call.Method.GetGenericArguments()[0] is Type grainType
                    && grainType == typeof(IAchievementManagerGrain)
                        ? manager
                        : FakeProxy.CreateFor(call.Method.GetGenericArguments()[0], _ => null)
                    : null
            );
        }

        /// <summary>Cumulative thresholds 5/20, so one level-up sits five entries in.</summary>
        private static AchievementDefinitionSnapshot Definition()
        {
            int[] thresholds = [5, 20];

            return new AchievementDefinitionSnapshot
            {
                Id = AchievementId,
                Name = RoomEntry,
                Category = "explore",
                DisplayMethod = 0,
                Levels = Enumerable
                    .Range(1, thresholds.Length)
                    .Select(level => new AchievementLevelSnapshot
                    {
                        Level = level,
                        BadgeCode = $"ACH_{RoomEntry}{level}",
                        ProgressRequirement = thresholds[level - 1],
                        RewardAmount = 0,
                        RewardType = 0,
                        ScorePoints = level,
                    })
                    .ToImmutableArray(),
            };
        }
    }

    private sealed class CountingDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public int Created { get; private set; }

        public VortexDbContext CreateDbContext()
        {
            Created++;

            return new VortexDbContext(options);
        }
    }
}
