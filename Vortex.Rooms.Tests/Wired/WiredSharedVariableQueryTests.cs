using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Which rooms fill the reference-variable dropdown.
/// <para>
/// The promise is narrow and it is the whole point of reading this from the database rather than
/// from the rooms: a player sees the variables their <em>own</em> rooms share, and nobody else's.
/// A room they merely hold rights in belongs to its owner and stays out.
/// </para>
/// </summary>
/// <remarks>
/// SQLite rather than the in-memory provider: the query joins the furni to its room and its
/// definition and filters on a logic-key set, and in-memory would run all of that in LINQ — it
/// cannot fail to translate, so it would prove nothing about the query that actually ships. Rows are
/// seeded in raw SQL because <c>created_at</c> is identity-generated and <c>updated_at</c> computed,
/// which MySQL fills from the migrations' column defaults and EnsureCreated on SQLite does not.
/// </remarks>
public sealed class WiredSharedVariableQueryTests : IAsyncLifetime
{
    private const int Owner = 1;
    private const int Stranger = 2;

    private const int OwnedRoom = 10;
    private const int StrangersRoom = 11;

    private const int RoomVariableDefinition = 100;
    private const int UserVariableDefinition = 101;
    private const int ChairDefinition = 102;

    private SqliteConnection _conn = null!;
    private DbContextOptions<VortexDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("Filename=:memory:");

        await _conn.OpenAsync();

        _options = new DbContextOptionsBuilder<VortexDbContext>().UseSqlite(_conn).Options;

        await using VortexDbContext db = new(_options);

        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");

        await SeedRoomAsync(db, OwnedRoom, Owner, "Mon appart");
        await SeedRoomAsync(db, StrangersRoom, Stranger, "Chez un autre");

        await SeedDefinitionAsync(db, RoomVariableDefinition, "wf_var_room");
        await SeedDefinitionAsync(db, UserVariableDefinition, "wf_var_user");
        await SeedDefinitionAsync(db, ChairDefinition, "default_floor");
    }

    public async Task DisposeAsync() => await _conn.DisposeAsync();

    [Fact]
    public async Task OnlyTheAskersOwnRoomsAreOffered()
    {
        await using VortexDbContext db = new(_options);

        await SeedBoxAsync(db, 900, OwnedRoom, RoomVariableDefinition, Shared("score"));
        await SeedBoxAsync(db, 901, StrangersRoom, RoomVariableDefinition, Shared("secret"));

        List<WiredVariableSharedSnapshot> shared = await WiredSharedVariableIndex.LoadForOwnerAsync(
            db,
            Owner,
            CancellationToken.None
        );

        shared.Select(v => v.Variable.VariableName).Should().Equal("score");
        shared.Single().RoomId.Should().Be(OwnedRoom);
        shared.Single().RoomName.Should().Be("Mon appart");
    }

    [Fact]
    public async Task OnlySharedVariableBoxesAreOffered()
    {
        await using VortexDbContext db = new(_options);

        await SeedBoxAsync(db, 910, OwnedRoom, RoomVariableDefinition, Shared("shared"));
        await SeedBoxAsync(
            db,
            911,
            OwnedRoom,
            UserVariableDefinition,
            Wired([(int)WiredAvailabilityType.Persistent, 1], "kept")
        );
        await SeedBoxAsync(db, 912, OwnedRoom, ChairDefinition, Shared("a chair"));

        // A box in the player's hand rather than in a room, and one that was thrown away: both hold
        // a shared variable and neither is in a room, so neither has a room to be offered under.
        await SeedBoxAsync(db, 913, null, RoomVariableDefinition, Shared("in the hand"));
        await SeedBoxAsync(db, 914, OwnedRoom, RoomVariableDefinition, Shared("deleted"), true);

        List<WiredVariableSharedSnapshot> shared = await WiredSharedVariableIndex.LoadForOwnerAsync(
            db,
            Owner,
            CancellationToken.None
        );

        shared.Select(v => v.Variable.VariableName).Should().Equal("shared");
    }

    [Fact]
    public async Task AFurniWhoseExtraDataIsNotJsonIsSkippedRatherThanFatal()
    {
        await using VortexDbContext db = new(_options);

        await SeedBoxAsync(db, 920, OwnedRoom, RoomVariableDefinition, "not json at all");
        await SeedBoxAsync(db, 921, OwnedRoom, RoomVariableDefinition, Shared("score"));

        List<WiredVariableSharedSnapshot> shared = await WiredSharedVariableIndex.LoadForOwnerAsync(
            db,
            Owner,
            CancellationToken.None
        );

        shared.Select(v => v.Variable.VariableName).Should().Equal("score");
    }

    [Fact]
    public async Task APlayerWhoSharesNothingGetsAnEmptyListRatherThanAFailure()
    {
        await using VortexDbContext db = new(_options);

        List<WiredVariableSharedSnapshot> shared = await WiredSharedVariableIndex.LoadForOwnerAsync(
            db,
            Owner,
            CancellationToken.None
        );

        shared.Should().BeEmpty();
    }

    private static string Shared(string name) => Wired([(int)WiredAvailabilityType.Shared], name);

    private static string Wired(List<int> intParams, string name) =>
        JsonSerializer.Serialize(
            new Dictionary<string, WiredData>
            {
                [ExtraDataSectionType.WIRED] = new() { IntParams = intParams, StringParam = name },
            }
        );

    private static Task SeedRoomAsync(VortexDbContext db, int id, int ownerId, string name) =>
        db.Database.ExecuteSqlRawAsync(
            "INSERT INTO rooms (id, name, player_id, model_id, last_active, wired_timezone, created_at, updated_at) "
                + "VALUES ({0}, {1}, {2}, 1, {3}, '', {3}, {3})",
            id,
            name,
            ownerId,
            DateTime.UtcNow
        );

    private static Task SeedDefinitionAsync(VortexDbContext db, int id, string logic) =>
        db.Database.ExecuteSqlRawAsync(
            "INSERT INTO furniture_definitions (id, sprite_id, name, logic, created_at, updated_at) "
                + "VALUES ({0}, {0}, {1}, {1}, {2}, {2})",
            id,
            logic,
            DateTime.UtcNow
        );

    private static Task SeedBoxAsync(
        VortexDbContext db,
        int id,
        int? roomId,
        int definitionId,
        string extraData,
        bool deleted = false
    ) =>
        db.Database.ExecuteSqlRawAsync(
            "INSERT INTO furniture (id, player_id, definition_id, room_id, extra_data, deleted_at, created_at, updated_at) "
                + $"VALUES ({{0}}, {{1}}, {{2}}, {roomId?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}, "
                + $"{{3}}, {(deleted ? "CURRENT_TIMESTAMP" : "NULL")}, {{4}}, {{4}})",
            id,
            Owner,
            definitionId,
            extraData,
            DateTime.UtcNow
        );
}
