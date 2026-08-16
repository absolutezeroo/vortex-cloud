using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Database.Entities.Security;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Benchmark;

/// <summary>
/// Builds the hotel a run needs, and takes it away again.
/// </summary>
/// <remarks>
/// <para>
/// Everything created here is named from <see cref="Marker"/>, and nothing is found any other way.
/// That is the whole safety story: teardown does not work from a list it was handed — a list is lost
/// when the process dies mid-run — it works from the marker, so a sweep at startup cleans up after a
/// crash exactly as well as the normal path cleans up after a success.
/// </para>
/// <para>
/// The accounts are real rows in <c>players</c>. They have to be: the login they perform is the real
/// one, and it reads that table. They are created with no perks, no currency and a status of
/// offline, and they are deleted whole afterwards.
/// </para>
/// </remarks>
internal sealed class BenchmarkProvisioner(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ILogger<BenchmarkProvisioner> logger
)
{
    /// <summary>
    /// The prefix that makes a row disposable. Chosen to be something no hotel would allow a player
    /// to register: the name check rejects it, so a real account can never collide with one of these
    /// and be swept away with them.
    /// </summary>
    public const string Marker = "__bench__";

    /// <summary>
    /// Written into <c>extra_data</c> on every item a run places. It is how the cleanup tells the
    /// furniture it added from the furniture that was already in a borrowed room — deleting by room
    /// would empty somebody's room, which is the worst thing this feature could possibly do.
    /// </summary>
    public const string FurnitureStamp = Marker + "item";

    private const string BenchFigure = "hd-180-1.ch-210-66.lg-270-82.sh-290-91";

    public async Task<BenchmarkFixture> ProvisionAsync(
        int players,
        int furniture,
        int targetRoomId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        bool borrowed = targetRoomId > 0;

        if (borrowed)
        {
            await CheckBorrowedRoomAsync(db, targetRoomId, players, ct).ConfigureAwait(false);
        }

        // The map of the room the run will use, so furniture lands on tiles that exist. Placing by
        // arithmetic instead -- a square grid from the origin -- puts most of a large batch on void
        // tiles, where the room ignores it: the rows are written, the items never appear, and the
        // run measures a room it thinks is full and is not.
        string modelData = borrowed
            ? await db
                .Rooms.AsNoTracking()
                .Where(r => r.Id == targetRoomId)
                .Select(r => r.RoomModelEntity!.Model)
                .FirstAsync(ct)
                .ConfigureAwait(false)
            : string.Empty;

        int modelId = 0;

        if (!borrowed)
        {
            // The roomiest model rather than the first one: model_a is mostly void, and a run asking
            // for two hundred items would have nowhere to put them.
            var model = await db
                .RoomModels.AsNoTracking()
                .Where(m => m.Enabled)
                .Select(m => new { m.Id, m.Model })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var roomiest = model
                .Select(m => new
                {
                    m.Id,
                    m.Model,
                    Tiles = OpenTiles(m.Model).Count,
                })
                .OrderByDescending(m => m.Tiles)
                .FirstOrDefault();

            if (roomiest is null || roomiest.Tiles == 0)
            {
                throw new InvalidOperationException("benchmark_no_room_model");
            }

            modelId = roomiest.Id;
            modelData = roomiest.Model;
        }

        List<(int X, int Y)> openTiles = OpenTiles(modelData);

        if (openTiles.Count == 0)
        {
            throw new InvalidOperationException("benchmark_room_has_no_floor");
        }

        // One definition, reused for every item. A run is measuring how much furniture costs, not
        // which furniture -- and a single definition keeps the client's side of the comparison
        // honest too, since sprite variety would change what it has to load.
        int definitionId = await db
            .FurnitureDefinitions.AsNoTracking()
            .Where(f => f.CanStack && f.SpriteId > 0)
            .OrderBy(f => f.Id)
            .Select(f => f.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (definitionId == 0 && furniture > 0)
        {
            throw new InvalidOperationException("benchmark_no_furniture_definition");
        }

        List<PlayerEntity> accounts = [];

        for (int index = 0; index < players; index++)
        {
            accounts.Add(
                new PlayerEntity
                {
                    Name = string.Create(CultureInfo.InvariantCulture, $"{Marker}{index:D4}"),
                    Figure = BenchFigure,
                    Gender = AvatarGenderType.Male,
                    PlayerStatus = PlayerStatusType.Offline,
                    PlayerPerks = PlayerPerkFlags.None,
                }
            );
        }

        db.Players.AddRange(accounts);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        PlayerEntity owner =
            accounts.FirstOrDefault()
            ?? throw new InvalidOperationException("benchmark_needs_one_player");

        int roomId = targetRoomId;

        if (!borrowed)
        {
            RoomEntity room = BuildRoom(owner, modelId);

            db.Rooms.Add(room);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            roomId = room.Id;
        }

        List<SecurityTicketEntity> tickets =
        [
            .. accounts.Select(account => new SecurityTicketEntity
            {
                PlayerEntityId = account.Id,
                PlayerEntity = account,
                Ticket = string.Create(CultureInfo.InvariantCulture, $"{Marker}{Guid.NewGuid():N}"),
                IpAddress = "127.0.0.1",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            }),
        ];

        db.SecurityTickets.AddRange(tickets);

        if (furniture > 0)
        {
            db.Furnitures.AddRange(
                BuildFurniture(roomId, owner.Id, definitionId, furniture, openTiles)
            );
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // The room reads its item map when it activates and not again, so furniture written under a
        // room that is already awake stays invisible -- the rows exist, the room never sees them,
        // and the run measures an empty room it believes is full. Sending it to sleep here means the
        // synthetic players wake it up and it loads everything, theirs and ours.
        await grainFactory
            .GetRoomCore(new RoomId(roomId))
            .DeactivateRoomAsync()
            .ConfigureAwait(false);

        logger.LogInformation(
            "Benchmark provisioned room {RoomId} with {Players} accounts and {Furniture} items.",
            roomId,
            accounts.Count,
            furniture
        );

        return new BenchmarkFixture
        {
            RoomId = roomId,
            Borrowed = borrowed,
            Tickets =
            [
                .. tickets.OrderBy(ticket => ticket.PlayerEntityId).Select(ticket => ticket.Ticket),
            ],
            PlacedFurniture = furniture,
            // Real tiles, so a walk is a walk. Sending a synthetic player at a hole means the room
            // refuses the move and the pathfinder -- the expensive half of the load -- never runs.
            WalkTargets = [.. openTiles.Take(32)],
        };
    }

    /// <summary>
    /// Refuses a borrowed room the run could not actually fill, before anything is created.
    /// </summary>
    /// <remarks>
    /// Both of these would otherwise fail quietly and produce a run that looks like it worked: the
    /// clients connect, the room turns them away one by one, and the samples show a hotel coping
    /// beautifully with almost no load. A refusal up front is worth more than a graph of nothing.
    /// </remarks>
    private static async Task CheckBorrowedRoomAsync(
        VortexDbContext db,
        int roomId,
        int players,
        CancellationToken ct
    )
    {
        var room = await db
            .Rooms.AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new { r.DoorMode, r.PlayersMax })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (room is null)
        {
            throw new InvalidOperationException("benchmark_room_not_found");
        }

        // A doorbell or a password stops a synthetic player at the door, and it has no way to knock.
        if (room.DoorMode != RoomDoorModeType.Open)
        {
            throw new InvalidOperationException("benchmark_room_not_open");
        }

        if (room.PlayersMax > 0 && room.PlayersMax < players)
        {
            throw new InvalidOperationException("benchmark_room_too_small");
        }
    }

    /// <summary>
    /// Removes everything the marker names, whether this process created it or a previous one did.
    /// Returns what it could not remove, which the caller reports rather than swallows.
    /// </summary>
    public async Task<string?> TeardownAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext db = await dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            List<int> playerIds = await db
                .Players.Where(p => p.Name.StartsWith(Marker))
                .Select(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (playerIds.Count == 0)
            {
                return null;
            }

            List<int> roomIds = await db
                .Rooms.Where(r => playerIds.Contains(r.PlayerEntityId))
                .Select(r => r.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Order matters: the furniture and the tickets point at the rooms and the players, and
            // MySQL will not let a row be deleted out from under a foreign key.
            // Stamped items first, wherever they ended up -- a borrowed room keeps everything else
            // it had. The room-scoped clause below only ever matches rooms the run created itself.
            await db
                .Furnitures.Where(f => f.ExtraData == FurnitureStamp)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            await db
                .Furnitures.Where(f =>
                    (f.RoomEntityId != null && roomIds.Contains(f.RoomEntityId.Value))
                    || playerIds.Contains(f.PlayerEntityId)
                )
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            await db
                .SecurityTickets.Where(t => playerIds.Contains(t.PlayerEntityId))
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            await db
                .Rooms.Where(r => roomIds.Contains(r.Id))
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            await db
                .Players.Where(p => playerIds.Contains(p.Id))
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Benchmark teardown removed {Players} accounts and {Rooms} rooms.",
                playerIds.Count,
                roomIds.Count
            );

            return null;
        }
        catch (Exception ex)
        {
            // Reported, never swallowed. Rows left behind are the one failure mode of this whole
            // feature that a player would eventually notice.
            logger.LogError(ex, "Benchmark teardown failed; rows may remain.");

            return ex.Message;
        }
    }

    private static RoomEntity BuildRoom(PlayerEntity owner, int modelId) =>
        new()
        {
            Name = Marker + "room",
            PlayerEntityId = owner.Id,
            PlayerEntity = owner,
            RoomModelEntityId = modelId,
            RoomModelEntity = null!,
            DoorMode = RoomDoorModeType.Open,
            UsersNow = 0,
            // Above anything a run will ask for: the room's own limit refusing the load would be a
            // measurement of the limit, not of the hotel.
            PlayersMax = 2000,
            WallHeight = -1,
            HideWalls = false,
            ThicknessWall = RoomThicknessType.Normal,
            ThicknessFloor = RoomThicknessType.Normal,
            AllowBlocking = true,
            AllowPets = false,
            AllowPetsEat = false,
            TradeType = RoomTradeModeType.Disabled,
            MuteType = ModSettingType.Owner,
            KickType = ModSettingType.Owner,
            BanType = ModSettingType.Owner,
            ChatModeType = ChatModeType.FreeFlow,
            ChatBubbleType = ChatBubbleWidthType.Normal,
            ChatSpeedType = ChatScrollSpeedType.Normal,
            ChatFloodType = ChatFloodSensitivityType.Minimal,
            ChatDistance = 50,
            Score = 0,
            IsStaffPick = false,
        };

    /// <summary>
    /// The tiles a room actually has floor on.
    /// </summary>
    /// <remarks>
    /// The heightmap is one character per tile, <c>x</c> for a hole, and the rows are separated by a
    /// literal backslash-n in the column rather than by a real newline — so all three separators are
    /// accepted here, and a model stored either way parses the same.
    /// </remarks>
    private static List<(int X, int Y)> OpenTiles(string model)
    {
        List<(int X, int Y)> tiles = [];

        if (string.IsNullOrWhiteSpace(model))
        {
            return tiles;
        }

        const string LiteralCarriageReturn = @"\r";
        const string LiteralNewline = @"\n";

        string[] rows = model
            .Replace(LiteralCarriageReturn, "\n", StringComparison.Ordinal)
            .Replace(LiteralNewline, "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y].Trim();

            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] is not ('x' or 'X' or ' '))
                {
                    tiles.Add((x, y));
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// Fills the floor first and only then stacks, which is what a crowded room looks like: a pile on
    /// one tile is a single lookup for the room and a single sprite column for the client, and would
    /// flatter both.
    /// </summary>
    private static IEnumerable<FurnitureEntity> BuildFurniture(
        int roomId,
        int ownerId,
        int definitionId,
        int count,
        List<(int X, int Y)> openTiles
    )
    {
        for (int index = 0; index < count; index++)
        {
            (int x, int y) = openTiles[index % openTiles.Count];

            yield return new FurnitureEntity
            {
                PlayerEntityId = ownerId,
                FurnitureDefinitionEntityId = definitionId,
                RoomEntityId = roomId,
                X = x,
                Y = y,
                Z = index / openTiles.Count,
                Rotation = Rotation.North,
                ExtraData = FurnitureStamp,
            };
        }
    }
}

internal sealed record BenchmarkFixture
{
    public required int RoomId { get; init; }

    public required ImmutableArray<string> Tickets { get; init; }

    public required int PlacedFurniture { get; init; }

    public required bool Borrowed { get; init; }

    public required ImmutableArray<(int X, int Y)> WalkTargets { get; init; }
}
