using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Sound;

/// <summary>
/// Where a song disk actually is once it has been loaded into a jukebox.
/// </summary>
/// <remarks>
/// <para>
/// A jukebox is the second place in this hotel where an item leaves its owner's hands without
/// leaving their ownership, and the first one — the wired chest — is where the duplication bugs
/// came from. The failure is not dramatic: the row moves, the inventory is never told, and the disk
/// is still on the player's screen to be placed. There is then furniture in a room and a disk in a
/// jukebox, both pointing at one row, and the second read of either wins.
/// </para>
/// <para>
/// On SQLite rather than the in-memory provider, because the move is an <c>ExecuteUpdate</c> and the
/// in-memory provider does not implement it — a test that cannot run the guard cannot vouch for it.
/// </para>
/// </remarks>
public sealed class JukeboxDiskMoveTests : IAsyncLifetime
{
    private const int OWNER = 7;
    private const int STRANGER = 8;
    private const int JUKEBOX = 500;
    private const int OTHER_JUKEBOX = 501;
    private const int DISK = 900;

    private SqliteConnection _conn = null!;
    private DbContextOptions<VortexDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        await _conn.OpenAsync();
        _options = new DbContextOptionsBuilder<VortexDbContext>().UseSqlite(_conn).Options;

        await using VortexDbContext db = new(_options);
        await db.Database.EnsureCreatedAsync();

        // Seeding the definition and the player would mean seeding most of the schema to test one
        // UPDATE guard, and referential integrity is not what is under test.
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");

        // Raw SQL because created_at is DatabaseGenerated(Identity) and updated_at is Computed, so
        // EF never writes either and EnsureCreated leaves both NOT NULL with no default.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO furniture (id, player_id, definition_id, x, y, z, direction, wall_offset,
                                   created_at, updated_at)
            VALUES ({0}, {1}, 1, 0, 0, 0, 0, 0, datetime('now'), datetime('now'))
            """,
            DISK,
            OWNER
        );
    }

    public async Task DisposeAsync() => await _conn.DisposeAsync();

    /// <summary>How many rows the owner's next inventory load would return.</summary>
    /// <remarks>The predicate is <c>InventoryFurnitureLoader</c>'s own, spelled out.</remarks>
    private async Task<int> ItemsInHandAsync()
    {
        await using VortexDbContext db = new(_options);

        return await db.Furnitures.CountAsync(f =>
            f.PlayerEntityId == OWNER
            && f.RoomEntityId == null
            && f.WiredChestEntityId == null
            && f.JukeboxEntityId == null
            && f.DeletedAt == null
        );
    }

    private async Task<int> LoadAsync(int playerId)
    {
        await using VortexDbContext db = new(_options);

        return await JukeboxDiskStore.LoadAsync(
            db,
            DISK,
            playerId,
            JUKEBOX,
            CancellationToken.None
        );
    }

    private async Task<int> UnloadAsync(int jukeboxId)
    {
        await using VortexDbContext db = new(_options);

        return await JukeboxDiskStore.UnloadAsync(db, DISK, jukeboxId, CancellationToken.None);
    }

    [Fact]
    public async Task ALoadedDisk_HasLeftItsOwnersHands()
    {
        (await ItemsInHandAsync()).Should().Be(1, "it is theirs to load");

        (await LoadAsync(OWNER)).Should().Be(1);

        (await ItemsInHandAsync())
            .Should()
            .Be(0, "the disk is in the jukebox; listing it as well is the duplication");
    }

    [Fact]
    public async Task LoadingTheSameDiskTwice_MovesItOnce()
    {
        (await LoadAsync(OWNER)).Should().Be(1);

        // The client can send the same request again — a double click, a reconnect, a replay. The
        // second one has to find nothing to move rather than write the jukebox id again over a row
        // that some other jukebox may by then own.
        (await LoadAsync(OWNER))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task ADiskThatIsNotYours_DoesNotMove()
    {
        (await LoadAsync(STRANGER)).Should().Be(0);

        (await ItemsInHandAsync()).Should().Be(1, "it never left the owner");
    }

    [Fact]
    public async Task UnloadingFromTheWrongJukebox_DoesNothing()
    {
        await LoadAsync(OWNER);

        (await UnloadAsync(OTHER_JUKEBOX)).Should().Be(0);

        (await ItemsInHandAsync()).Should().Be(0, "the disk is still in the jukebox it went into");
    }

    [Fact]
    public async Task UnloadingGivesTheDiskBackToItsOwner_NotToWhoeverTookItOut()
    {
        await LoadAsync(OWNER);

        (await UnloadAsync(JUKEBOX)).Should().Be(1);

        (await ItemsInHandAsync()).Should().Be(1);

        await using VortexDbContext db = new(_options);

        // Ownership is untouched by the round trip: emptying a jukebox is not a way to collect the
        // disks other people put in it.
        (await db.Furnitures.SingleAsync(f => f.Id == DISK))
            .PlayerEntityId.Should()
            .Be(OWNER);
    }

    [Fact]
    public async Task ThePlaylistReadsInInsertionOrder_AndStopsAtTheCapacity()
    {
        await using (VortexDbContext seed = new(_options))
        {
            await seed.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO furniture (id, player_id, definition_id, jukebox_id, x, y, z, direction,
                                       wall_offset, created_at, updated_at)
                VALUES (901, {0}, 1, {1}, 0, 0, 0, 0, 0, datetime('now'), datetime('now')),
                       (902, {0}, 1, {1}, 0, 0, 0, 0, 0, datetime('now'), datetime('now')),
                       (903, {0}, 1, {1}, 0, 0, 0, 0, 0, datetime('now'), datetime('now'))
                """,
                OWNER,
                JUKEBOX
            );
        }

        await using VortexDbContext db = new(_options);

        List<JukeboxDiskRow> rows = await JukeboxDiskStore.ReadAsync(
            db,
            JUKEBOX,
            limit: 2,
            CancellationToken.None
        );

        rows.Should().HaveCount(2);
        rows[0].DiskId.Should().Be(901);
        rows[1].DiskId.Should().Be(902);
        rows[0].OwnerId.Should().Be(OWNER);
    }
}
