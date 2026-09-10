using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Database.Tests.Support;

/// <summary>
/// An in-memory SQLite database carrying the Vortex schema.
/// </summary>
/// <remarks>
/// <para>
/// The in-memory EF provider cannot express half of what the ownership paths write: it does not
/// implement <c>ExecuteUpdate</c>, which is how the trade, the mint, the marketplace and the room
/// flush all claim a row. A suite on that provider cannot run the guard it is vouching for -- it
/// throws, and a grain that catches its own exceptions turns that into a green test over a write
/// that never happened.
/// </para>
/// <para>
/// The schema needs one repair on the way in. Every <c>VortexEntity</c> declares <c>created_at</c>
/// as <c>DatabaseGenerated(Identity)</c> and <c>updated_at</c> as <c>Computed</c>, so EF writes
/// neither and leaves both to the database. MySQL's migrations give them column defaults; the
/// schema EF derives for SQLite gives them NOT NULL and nothing else, so any insert of a
/// <c>VortexEntity</c> fails there. The create script gets those defaults before it runs.
/// </para>
/// </remarks>
internal static class SqliteTestDb
{
    /// <summary>
    /// Opens a database and returns the connection that owns it -- an in-memory SQLite database
    /// lives exactly as long as its connection, so the caller has to hold this and dispose it.
    /// </summary>
    public static async Task<(
        SqliteConnection Connection,
        DbContextOptions<VortexDbContext> Options
    )> OpenAsync()
    {
        SqliteConnection connection = new("Filename=:memory:");
        await connection.OpenAsync();

        DbContextOptions<VortexDbContext> options = new DbContextOptionsBuilder<VortexDbContext>()
            .UseSqlite(connection)
            .Options;

        await using VortexDbContext db = new(options);

        // Matched by column name rather than by type: the repair should not care whether EF renders
        // a DateTime as TEXT or datetime for a given provider version.
        string script = Regex.Replace(
            db.Database.GenerateCreateScript(),
            """("(?:created_at|updated_at)"\s+[^\s,]+)\s+NOT NULL""",
            "$1 NOT NULL DEFAULT CURRENT_TIMESTAMP"
        );

        await db.Database.ExecuteSqlRawAsync(script);

        // Seeding a definition and a player would mean seeding most of the schema to test one
        // write, and referential integrity is not what these suites are about.
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");

        return (connection, options);
    }

    /// <summary>A free-standing furniture row: owned, in no room, in no chest, in no jukebox --
    /// which is the shape every ownership claim tests for.</summary>
    public static async Task SeedFurnitureAsync(
        DbContextOptions<VortexDbContext> options,
        int itemId,
        int ownerId,
        int definitionId = 1
    )
    {
        await using VortexDbContext db = new(options);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO furniture (id, player_id, definition_id, room_id, x, y, z, direction,
                                   wall_offset, extra_data, created_at, updated_at)
            VALUES ({0}, {1}, {2}, NULL, 0, 0, 0, 0, 0, '', datetime('now'), datetime('now'))
            """,
            itemId,
            ownerId,
            definitionId
        );
    }
}
