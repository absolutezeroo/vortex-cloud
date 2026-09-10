using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Pets;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Grains;

/// <summary>
/// What a room can lose, and for how long.
/// </summary>
/// <remarks>
/// <para>
/// Furniture positions are memory-first: moving a sofa writes to the room's own state and queues a
/// snapshot, which <see cref="RoomPersistenceGrain"/> writes every <c>DirtyItemsTickMs</c>. A crash
/// inside that window costs the moves made in it, and that is a deliberate trade — a database write
/// per drag would put the hotel's furniture on the hot path of every build session.
/// </para>
/// <para>
/// Two things were not part of that trade. A flush whose save failed had already taken its batch off
/// the queue, so a connection blip lost those positions permanently rather than for two seconds; and
/// a deactivating room wrote one batch and abandoned the rest, so any room with more than
/// <c>MaxDirtyItemsPerFlush</c> pending moves lost the overflow every time it went to sleep. Both are
/// unbounded loss dressed up as a bounded window, which is the version nobody notices.
/// </para>
/// </remarks>
public sealed class RoomPersistenceLossWindowTests
{
    private const long ROOM = 7;

    [Fact]
    public async Task AFailedFlushKeepsItsBatchForTheNextOne()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueueAsync(1);

        h.Fail = true;
        await h.DeactivateAsync();

        h.RowsInRoom().Should().Be(0, "the save failed");

        // The regression: the batch used to be dropped from the queue before the try, so this second
        // attempt had nothing left to write and the move was gone for good.
        h.Fail = false;
        await h.DeactivateAsync();

        h.RowsInRoom().Should().Be(1);
    }

    /// <summary>
    /// Deactivation drains, rather than writing one batch and abandoning the rest. The cap belongs to
    /// the timer — it stops one busy room holding a connection through a whole build session — and
    /// has no business deciding how much of a closing room survives.
    /// </summary>
    [Fact]
    public async Task DeactivationDrainsEveryPendingBatch()
    {
        using Harness h = new(maxPerFlush: 2);

        await h.EnqueueAsync(1, 2, 3, 4, 5);
        await h.DeactivateAsync();

        h.RowsInRoom().Should().Be(5);
    }

    /// <summary>
    /// And it stops. A drain that loops until the queue empties would spin forever against a database
    /// that is refusing writes — during deactivation, which is where a silo is least able to say so.
    /// </summary>
    [Fact]
    public async Task DeactivationAgainstADeadDatabaseStopsAfterOnePass()
    {
        using Harness h = new(maxPerFlush: 2) { Fail = true };

        await h.EnqueueAsync(1, 2, 3, 4, 5);

        Func<Task> deactivate = () => h.DeactivateAsync();

        await deactivate.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
        h.RowsInRoom().Should().Be(0);
    }

    /// <summary>
    /// Two rooms, one row. Pick a sofa up in A and drop it in B inside <c>DirtyItemsTickMs</c>: B
    /// claims the row at once, and A's flush was then writing <c>RoomEntityId = null</c> over the
    /// claim, so the sofa vanished from B (ROOM-PER-005). A removal only applies while the row is
    /// still this room's.
    /// </summary>
    [Fact]
    public async Task ARemovalDoesNotUnseatARoomThatAlreadyClaimedTheItem()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueueRemovalAsync(1);
        h.ClaimByAnotherRoom(1, room: 99);

        await h.DeactivateAsync();

        h.RoomOf(1).Should().Be(99);
    }

    [Fact]
    public async Task ARemovalOfAnItemStillInTheRoom_TakesItOut()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueueAsync(1);
        await h.DeactivateAsync();

        h.RoomOf(1).Should().Be((int)ROOM);

        await h.EnqueueRemovalAsync(1);
        await h.DeactivateAsync();

        h.RoomOf(1).Should().BeNull();
    }

    /// <summary>
    /// Pick a sofa up and put it straight back down, inside one <c>DirtyItemsTickMs</c>. The removal
    /// marker and the snapshot live in two collections and only the snapshot was being overwritten,
    /// so the flush read "removed" over a snapshot that says placed and wrote
    /// <c>RoomEntityId = null</c> on furniture the player was standing next to. It came back in the
    /// inventory on the next room load.
    /// </summary>
    [Fact]
    public async Task APickupFollowedByAReplacement_LeavesTheItemInTheRoom()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueueRemovalAsync(1);
        await h.EnqueueAsync(1);

        await h.DeactivateAsync();

        h.RoomOf(1).Should().Be((int)ROOM);
    }

    /// <summary>
    /// And through the room's own tick, which is the path a replacement actually takes:
    /// <c>MarkDirty</c> collects the item and <c>RoomGrain.FlushDirtyItemsAsync</c> hands the batch
    /// over as a bulk enqueue — a different method, which had the same gap.
    /// </summary>
    [Fact]
    public async Task ARoomTickAfterAPickup_ClearsTheRemovalToo()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueueRemovalAsync(1);
        await h.EnqueueBatchAsync(1);

        await h.DeactivateAsync();

        h.RoomOf(1).Should().Be((int)ROOM);
    }

    /// <summary>
    /// The other direction still works: a replacement followed by a pickup is a pickup. Only the
    /// newest word about the item counts, whichever way round it came.
    /// </summary>
    [Fact]
    public async Task AReplacementFollowedByAPickup_TakesTheItemOut()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueueAsync(1);
        await h.DeactivateAsync();

        await h.EnqueueBatchAsync(1);
        await h.EnqueueRemovalAsync(1);
        await h.DeactivateAsync();

        h.RoomOf(1).Should().BeNull();
    }

    /// <summary>
    /// PET-TICK-044: the pet stats used to be written by the room itself, from inside its tick.
    /// They come here now, on the clock that already writes the furniture.
    /// </summary>
    [Fact]
    public async Task EnqueuedPets_AreWrittenOnTheSameFlush()
    {
        using Harness h = new(maxPerFlush: 100);

        await h.EnqueuePetAsync(petId: 1, nutrition: 42);
        await h.DeactivateAsync();

        h.PetNutrition(1).Should().Be(42);
    }

    [Fact]
    public async Task APetTheDatabaseRefused_IsKeptForTheNextFlush()
    {
        using Harness h = new(maxPerFlush: 100) { Fail = true };

        await h.EnqueuePetAsync(petId: 1, nutrition: 42);
        await h.DeactivateAsync();

        h.PetNutrition(1).Should().Be(0, "the save never happened");

        h.Fail = false;
        await h.DeactivateAsync();

        h.PetNutrition(1).Should().Be(42, "and the queue still had it");
    }

    /// <remarks>
    /// SQLite rather than the InMemory provider, which every other grain suite here uses. The
    /// removal is a conditional claim -- one UPDATE carrying its own guard -- and InMemory does not
    /// implement <c>ExecuteUpdate</c>: it applied nothing and said nothing, so the two removal tests
    /// went green against a flush that had written no rows at all. A suite about a race between two
    /// writers needs a provider that can express the write.
    /// </remarks>
    private sealed class Harness : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly RoomPersistenceGrain _grain;
        private readonly DbContextOptions<VortexDbContext> _options;

        /// <summary>Makes the context factory throw, which is what a database blip looks like here.</summary>
        public bool Fail { get; set; }

        public Harness(int maxPerFlush)
        {
            _conn = new SqliteConnection("Filename=:memory:");
            _conn.Open();

            _options = new DbContextOptionsBuilder<VortexDbContext>().UseSqlite(_conn).Options;

            using (VortexDbContext seed = new(_options))
            {
                seed.Database.EnsureCreated();

                // Seeding a definition and a player would mean seeding most of the schema to test
                // one UPDATE guard, and referential integrity is not what is under test.
                seed.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");

                // Raw SQL because created_at is DatabaseGenerated(Identity) and updated_at is
                // Computed, so EF writes neither and EnsureCreated leaves both NOT NULL with no
                // default. Updates are unaffected, which is all the flush does afterwards.
                //
                // The flush issues updates, so the rows have to exist. They start outside the room,
                // which is what makes "was it written" answerable.
                for (int id = 1; id <= 8; id++)
                {
                    seed.Database.ExecuteSqlRaw(
                        """
                        INSERT INTO furniture (id, player_id, definition_id, room_id, x, y, z,
                                               direction, wall_offset, extra_data,
                                               created_at, updated_at)
                        VALUES ({0}, 1, 1, NULL, 0, 0, 0, 0, 0, '', datetime('now'), datetime('now'))
                        """,
                        id
                    );
                }
            }

            _grain = GrainActivationContext.CreateWithIntegerKey<RoomPersistenceGrain>(
                ROOM,
                FakeProxy.Create<IDbContextFactory<VortexDbContext>>(call =>
                    Fail ? throw new InvalidOperationException("database unavailable")
                    : call.Method.Name == nameof(IDbContextFactory<VortexDbContext>.CreateDbContext)
                        ? new VortexDbContext(_options)
                    : Task.FromResult(new VortexDbContext(_options))
                ),
                Options.Create(
                    new RoomConfig { DirtyItemsTickMs = 2000, MaxDirtyItemsPerFlush = maxPerFlush }
                ),
                NullLogger<IRoomPersistenceGrain>.Instance
            );
        }

        public async Task EnqueueAsync(params int[] objectIds)
        {
            foreach (int objectId in objectIds)
            {
                await _grain.EnqueueDirtyItemAsync(
                    new RoomId((int)ROOM),
                    Snapshot(objectId),
                    CancellationToken.None
                );
            }
        }

        /// <summary>
        ///     Deactivation, which is the only flush reachable from outside the grain — the timer never
        ///     fires outside a silo. It drains, so it exercises the timer's flush as well.
        /// </summary>
        public Task DeactivateAsync() => _grain.OnDeactivateAsync(Reason(), CancellationToken.None);

        /// <summary>Seeds a pet row and hands the grain a snapshot of it with new stats.</summary>
        public async Task EnqueuePetAsync(int petId, int nutrition)
        {
            using (VortexDbContext db = new(_options))
            {
                // Raw SQL for the same reason the furniture rows are: EF writes neither timestamp
                // and EnsureCreated leaves both NOT NULL without a default.
                db.Database.ExecuteSqlRaw(
                    """
                    INSERT INTO pets (id, player_id, room_id, name, type, race, color, gender,
                                      level, experience, energy, nutrition, thirst, respect,
                                      happiness, respect_today_count, rarity_level, can_breed,
                                      has_saddle, riding_permission, x, y, z, direction,
                                      created_at, updated_at)
                    VALUES ({0}, 1, NULL, 'pet', 0, 0, 'ffffff', 0,
                            1, 0, 0, 0, 100, 0,
                            100, 0, 1, 1,
                            0, 0, 0, 0, 0, 0,
                            datetime('now'), datetime('now'))
                    """,
                    petId
                );
            }

            await _grain.EnqueueDirtyPetsAsync(
                new RoomId((int)ROOM),
                [PetSnapshotWith(petId, nutrition)],
                CancellationToken.None
            );
        }

        public int PetNutrition(int petId)
        {
            using VortexDbContext db = new(_options);

            return db.Pets.Single(pet => pet.Id == petId).Nutrition;
        }

        /// <summary>
        /// The room's own tick path: one batch of snapshots of items that are in the room. Separate
        /// from <see cref="EnqueueAsync" /> because it is a separate method on the grain, and the
        /// removal marker had to be cleared on both.
        /// </summary>
        public Task EnqueueBatchAsync(params int[] objectIds) =>
            _grain.EnqueueDirtyItemsAsync(
                new RoomId((int)ROOM),
                [.. objectIds.Select(Snapshot)],
                CancellationToken.None
            );

        public Task EnqueueRemovalAsync(int objectId) =>
            _grain.EnqueueDirtyItemAsync(
                new RoomId((int)ROOM),
                Snapshot(objectId),
                CancellationToken.None,
                remove: true
            );

        /// <summary>The other room getting there first, which is all "dropped in B" looks like from
        /// here: one row, claimed.</summary>
        public void ClaimByAnotherRoom(int objectId, int room)
        {
            using VortexDbContext db = new(_options);

            db.Set<FurnitureEntity>().Single(f => f.Id == objectId).RoomEntityId = room;
            db.SaveChanges();
        }

        public int? RoomOf(int objectId)
        {
            using VortexDbContext db = new(_options);

            return db.Set<FurnitureEntity>().Single(f => f.Id == objectId).RoomEntityId;
        }

        public int RowsInRoom()
        {
            using VortexDbContext db = new(_options);

            return db.Set<FurnitureEntity>().Count(f => f.RoomEntityId == (int)ROOM);
        }

        private static PetSnapshot PetSnapshotWith(int petId, int nutrition) =>
            new()
            {
                PetId = petId,
                OwnerId = new PlayerId(1),
                RoomId = (int)ROOM,
                Name = "pet",
                Type = 0,
                Race = 0,
                Color = "ffffff",
                Gender = AvatarGenderType.Male,
                Level = 1,
                Experience = 0,
                Energy = 0,
                Nutrition = nutrition,
                Respect = 0,
                X = 0,
                Y = 0,
                Z = 0,
                Direction = Rotation.South,
            };

        private static DeactivationReason Reason() =>
            new(DeactivationReasonCode.ShuttingDown, "test");

        private static RoomFloorItemSnapshot Snapshot(int objectId) =>
            new()
            {
                ObjectId = new RoomObjectId(objectId),
                OwnerId = new PlayerId(1),
                OwnerName = "owner",
                DefinitionId = 1,
                SpriteId = 1,
                X = objectId,
                Y = objectId,
                Z = Altitude.FromInt(0),
                Rotation = Rotation.North,
                StackHeight = Altitude.FromInt(1),
                StuffData = new EmptyStuffSnapshot { StuffBitmask = 0 },
                ExtraData = "",
                UsagePolicy = FurnitureUsageType.Nobody,
            };

        /// <summary>An in-memory SQLite database lives exactly as long as its connection.</summary>
        public void Dispose() => _conn.Dispose();
    }
}
