using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
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
        Harness h = new(maxPerFlush: 100);

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
        Harness h = new(maxPerFlush: 2);

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
        Harness h = new(maxPerFlush: 2) { Fail = true };

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
        Harness h = new(maxPerFlush: 100);

        await h.EnqueueRemovalAsync(1);
        h.ClaimByAnotherRoom(1, room: 99);

        await h.DeactivateAsync();

        h.RoomOf(1).Should().Be(99);
    }

    [Fact]
    public async Task ARemovalOfAnItemStillInTheRoom_TakesItOut()
    {
        Harness h = new(maxPerFlush: 100);

        await h.EnqueueAsync(1);
        await h.DeactivateAsync();

        h.RoomOf(1).Should().Be((int)ROOM);

        await h.EnqueueRemovalAsync(1);
        await h.DeactivateAsync();

        h.RoomOf(1).Should().BeNull();
    }

    private sealed class Harness
    {
        private readonly RoomPersistenceGrain _grain;
        private readonly DbContextOptions<VortexDbContext> _options;

        /// <summary>Makes the context factory throw, which is what a database blip looks like here.</summary>
        public bool Fail { get; set; }

        public Harness(int maxPerFlush)
        {
            _options = new DbContextOptionsBuilder<VortexDbContext>()
                .UseInMemoryDatabase($"persistence-{Guid.NewGuid()}")
                .Options;

            using (VortexDbContext seed = new(_options))
            {
                // The flush issues updates, so the rows have to exist. They start outside the room,
                // which is what makes "was it written" answerable.
                for (int id = 1; id <= 8; id++)
                {
                    seed.Add(
                        new FurnitureEntity
                        {
                            Id = id,
                            PlayerEntityId = 1,
                            FurnitureDefinitionEntityId = 1,
                            RoomEntityId = null,
                        }
                    );
                }

                seed.SaveChanges();
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
    }
}
