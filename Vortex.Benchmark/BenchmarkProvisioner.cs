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
using Vortex.Database.Entities.Messenger;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Database.Entities.Security;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Furniture.Enums;
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

    private const string BenchFigure = "hd-180-1.ch-210-66.lg-270-82.sh-290-91";

    /// <summary>
    /// What each bench account is given in every enabled currency. Large enough that a five-minute
    /// run never runs dry — a bot that goes broke stops exercising the purchase path and starts
    /// exercising the refusal path, at the same rate, with nothing in the report to say so.
    /// </summary>
    private const int BenchBalance = 100_000_000;

    /// <summary>
    /// How much empty floor a run needs before it is worth starting. Below this the walk load is
    /// a stream of refusals rather than movement, which the report cannot tell apart from walking.
    /// </summary>
    private const int MinWalkTargets = 8;

    public async Task<BenchmarkFixture> ProvisionAsync(
        int players,
        int furniture,
        int targetRoomId,
        ImmutableArray<int> definitionIds,
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
        var borrowedModel = borrowed
            ? await db
                .Rooms.AsNoTracking()
                .Where(r => r.Id == targetRoomId)
                .Select(r => new { r.RoomModelEntity!.Model, r.RoomModelEntity!.Name })
                .FirstAsync(ct)
                .ConfigureAwait(false)
            : null;

        string modelData = borrowedModel?.Model ?? string.Empty;

        // Carried even for a borrowed room: a synthetic player creating a room of its own names a
        // model, and the one this run is standing in is the one known to be loadable here.
        string modelName = borrowedModel?.Name ?? string.Empty;

        int modelId = 0;

        if (!borrowed)
        {
            // The roomiest model rather than the first one: model_a is mostly void, and a run asking
            // for two hundred items would have nowhere to put them.
            var model = await db
                .RoomModels.AsNoTracking()
                .Where(m => m.Enabled)
                .Select(m => new
                {
                    m.Id,
                    m.Model,
                    m.Name,
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var roomiest = model
                .Select(m => new
                {
                    m.Id,
                    m.Model,
                    m.Name,
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
            modelName = roomiest.Name;
        }

        List<(int X, int Y)> openTiles = OpenTiles(modelData);

        if (openTiles.Count == 0)
        {
            throw new InvalidOperationException("benchmark_room_has_no_floor");
        }

        List<int> definitions = await ResolveDefinitionsAsync(db, definitionIds, furniture, ct)
            .ConfigureAwait(false);

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

        // Held rather than streamed straight into AddRange: the run needs these rows' ids afterwards
        // to tell a synthetic player which item to move, and an id only exists once EF has saved.
        List<FurnitureEntity> placed =
            furniture > 0
                ? [.. BuildFurniture(roomId, owner.Id, definitions, furniture, openTiles)]
                : [];

        if (placed.Count > 0)
        {
            db.Furnitures.AddRange(placed);
        }

        await GrantCurrenciesAsync(db, accounts, ct).ConfigureAwait(false);

        BuildFriendships(db, accounts);

        // Rights on the run's own room, and never on a borrowed one. Only the owner may move
        // furniture, so without this every drag by the other hundred and forty-nine players was
        // refused — three and a half thousand times in one run, each one an ERR with a stack trace.
        //
        // Withheld for a borrowed room on purpose: that room is yours, and a bench account with
        // rights on it could rearrange your furniture. The run then measures no moves there, which
        // is the honest trade — a borrowed room is for measuring load, not for editing.
        if (!borrowed)
        {
            db.RoomRights.AddRange(
                accounts.Select(account => new RoomRightEntity
                {
                    RoomEntityId = roomId,
                    PlayerEntityId = account.Id,
                })
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

        ImmutableArray<(int PageId, int OfferId)> offers = await ResolveOffersAsync(db, ct)
            .ConfigureAwait(false);

        // Tiles nothing is standing on. BuildFurniture fills openTiles from the front, one item per
        // tile, so the free floor starts where it stopped — and a borrowed room's own items block
        // tiles this run never chose, which is why they are subtracted too.
        //
        // Handing out occupied tiles as walk targets is what every run before this one did: the goal
        // was unoccupiable, the room refused the move, and the pathfinder's refusal was counted as
        // walking. Twenty thousand times in six minutes, in a report that said the run went fine.
        HashSet<(int X, int Y)> taken = [.. openTiles.Take(Math.Min(furniture, openTiles.Count))];

        if (borrowed)
        {
            List<(int X, int Y)> existing = await db
                .Furnitures.AsNoTracking()
                .Where(f => f.RoomEntityId == roomId)
                .Select(f => new ValueTuple<int, int>(f.X, f.Y))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            taken.UnionWith(existing);
        }

        List<(int X, int Y)> freeTiles = [.. openTiles.Where(tile => !taken.Contains(tile))];

        if (freeTiles.Count < MinWalkTargets)
        {
            // Refused rather than measured. A room with no floor left cannot be walked in, and a run
            // that reports a walk load it never applied is worse than one that will not start.
            throw new InvalidOperationException("benchmark_room_too_full");
        }

        return new BenchmarkFixture
        {
            RoomId = roomId,
            Borrowed = borrowed,
            Tickets =
            [
                .. tickets.OrderBy(ticket => ticket.PlayerEntityId).Select(ticket => ticket.Ticket),
            ],
            PlacedFurniture = furniture,
            // Real tiles, empty ones, so a walk is a walk. Sending a synthetic player at a hole --
            // or at a tile this run just filled with a sofa -- means the room refuses the move and
            // the pathfinder, the expensive half of the load, never runs.
            WalkTargets = [.. freeTiles.Take(32)],
            FurnitureIds = [.. placed.Select(item => item.Id)],
            // Ticket order, so index N of the plan is the player whose ticket is index N: the drive
            // loop picks a correspondent by index and must not be writing to a stranger.
            PlayerIds = [.. accounts.OrderBy(account => account.Id).Select(account => account.Id)],
            CatalogOffers = offers,
            RoomModelName = modelName,
        };
    }

    /// <summary>
    /// Turns the requested definition ids into a list the run can place, or explains why it cannot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only floor items, only one tile square. Both refusals come from the same lesson: the first
    /// version took whatever was first in the table, which was a wall-mounted post-it, and wrote two
    /// hundred of them at floor coordinates. They existed in the database and nowhere else.
    /// </para>
    /// <para>
    /// A bigger item would overlap its neighbours once the floor is tiled one item per tile, and an
    /// overlapping item is refused by the room — the same invisible failure wearing a different hat.
    /// </para>
    /// </remarks>
    private static async Task<List<int>> ResolveDefinitionsAsync(
        VortexDbContext db,
        ImmutableArray<int> requested,
        int furniture,
        CancellationToken ct
    )
    {
        if (furniture == 0)
        {
            return [];
        }

        IQueryable<FurnitureDefinitionEntity> placeable = db
            .FurnitureDefinitions.AsNoTracking()
            .Where(f =>
                f.ProductType == ProductType.Floor
                && f.SpriteId > 0
                && f.Width == 1
                && f.Length == 1
            );

        if (requested.IsEmpty)
        {
            int fallback = await placeable
                .Where(f => f.Logic == "furniture_basic" || f.Logic == "none")
                .OrderBy(f => f.Id)
                .Select(f => f.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return fallback == 0
                ? throw new InvalidOperationException("benchmark_no_furniture_definition")
                : [fallback];
        }

        List<int> resolved = await placeable
            .Where(f => requested.Contains(f.Id))
            .Select(f => f.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (resolved.Count != requested.Length)
        {
            // Named rather than silently dropped: a run that quietly placed half of what was asked
            // for would be a measurement of something nobody chose.
            throw new InvalidOperationException("benchmark_furniture_not_placeable");
        }

        return resolved;
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
            // Everything a bench account owns, wherever it ended up -- which is how a borrowed room
            // keeps every item that was already in it and loses only what the run added.
            await db
                .Furnitures.Where(f =>
                    (f.RoomEntityId != null && roomIds.Contains(f.RoomEntityId.Value))
                    || playerIds.Contains(f.PlayerEntityId)
                )
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            // A friendship names a player at BOTH ends, so matching only the owning side leaves the
            // mirror row behind — and the player delete below then fails on its foreign key, which
            // is the one way this sweep turns a tidy run into rows nobody can remove by hand.
            await db
                .MessengerFriends.Where(f =>
                    playerIds.Contains(f.PlayerEntityId)
                    || playerIds.Contains(f.FriendPlayerEntityId)
                )
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            await db
                .PlayerCurrencies.Where(c => playerIds.Contains(c.PlayerEntityId))
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            // Both ends again: a right names a player and a room, and the run grants one per account
            // on its own room. Matched on either side so a borrowed room cannot keep a bench grant.
            await db
                .RoomRights.Where(r =>
                    playerIds.Contains(r.PlayerEntityId) || roomIds.Contains(r.RoomEntityId)
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

    /// <summary>
    /// Gives every bench account a large balance in every enabled currency.
    /// </summary>
    /// <remarks>
    /// Every enabled type rather than "the credits one", because a wallet credit is a no-op without
    /// a matching <c>currency_types</c> row: granting a currency the hotel does not define succeeds
    /// silently and buys nothing. Iterating the rows that exist is what makes a purchase reach the
    /// code this run means to measure, whatever an offer happens to be priced in.
    /// </remarks>
    private static async Task GrantCurrenciesAsync(
        VortexDbContext db,
        List<PlayerEntity> accounts,
        CancellationToken ct
    )
    {
        // Every type, not only the enabled ones. The filter was there out of tidiness and it cost a
        // thousand failed debits: an offer may be priced in a currency the hotel does not currently
        // advertise, and the wallet then finds no row to subtract from and throws.
        List<int> currencyTypeIds = await db
            .CurrencyTypes.AsNoTracking()
            .Select(type => type.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (PlayerEntity account in accounts)
        {
            foreach (int currencyTypeId in currencyTypeIds)
            {
                db.PlayerCurrencies.Add(
                    new PlayerCurrencyEntity
                    {
                        PlayerEntityId = account.Id,
                        CurrencyTypeEntityId = currencyTypeId,
                        Amount = BenchBalance,
                    }
                );
            }
        }
    }

    /// <summary>
    /// Wires the accounts into a ring of friendships so every one of them has somebody to write to.
    /// </summary>
    /// <remarks>
    /// Both directions of each pair, not one: the messenger checks the sender's own friend list
    /// before it delivers, so a one-sided row would turn every message into a refusal — and the run
    /// would measure the rejection path at full speed while the report said "messages sent".
    /// </remarks>
    private static void BuildFriendships(VortexDbContext db, List<PlayerEntity> accounts)
    {
        if (accounts.Count < 2)
        {
            return;
        }

        for (int index = 0; index < accounts.Count; index++)
        {
            PlayerEntity left = accounts[index];
            PlayerEntity right = accounts[(index + 1) % accounts.Count];

            db.MessengerFriends.Add(Friendship(left, right));
            db.MessengerFriends.Add(Friendship(right, left));
        }

        static MessengerFriendEntity Friendship(PlayerEntity owner, PlayerEntity friend) =>
            new()
            {
                PlayerEntityId = owner.Id,
                PlayerEntity = owner,
                FriendPlayerEntityId = friend.Id,
                FriendPlayerEntity = friend,
                RelationType = MessengerFriendRelationType.Zero,
            };
    }

    /// <summary>
    /// The page and offer pairs a bench account is allowed to buy and can afford.
    /// </summary>
    /// <remarks>
    /// Visible, no club requirement, and carrying at least one product. Each of those is a refusal
    /// the catalogue makes before reaching the wallet, and a run buying nothing but refusals looks
    /// identical in the report to a run buying successfully — same packets, same rate, no error.
    /// </remarks>
    private static async Task<ImmutableArray<(int PageId, int OfferId)>> ResolveOffersAsync(
        VortexDbContext db,
        CancellationToken ct
    )
    {
        var offers = await db
            .CatalogOffers.AsNoTracking()
            .Where(offer =>
                offer.Visible
                && offer.ClubLevel == 0
                && offer.Products!.Count > 0
                // Priced in credits alone. Ordering by CostCredits without this picked offers whose
                // real price was in activity points, and the wallet refused every one of them: the
                // debit found no matching currency for the player and reported "changed 0".
                && offer.CostCurrency == 0
                && offer.CurrencyTypeId == null
            )
            .OrderBy(offer => offer.CostCredits)
            .Select(offer => new { offer.CatalogPageEntityId, offer.Id })
            .Take(16)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return [.. offers.Select(offer => (offer.CatalogPageEntityId, offer.Id))];
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
        List<int> definitions,
        int count,
        List<(int X, int Y)> openTiles
    )
    {
        // Deterministic on purpose. Two runs of the same plan lay the room out identically, which is
        // what makes their numbers comparable -- a random layout would move the answer between runs
        // and there would be no way to tell that from a change in the code.
        for (int index = 0; index < count; index++)
        {
            (int x, int y) = openTiles[index % openTiles.Count];

            yield return new FurnitureEntity
            {
                PlayerEntityId = ownerId,
                // Interleaved rather than one block per definition: a real room mixes its sprites,
                // and the client's cost is per sprite drawn, not per sprite loaded.
                FurnitureDefinitionEntityId = definitions[index % definitions.Count],
                RoomEntityId = roomId,
                X = x,
                Y = y,
                Z = index / openTiles.Count,
                Rotation = Rotation.North,
                // Left empty on purpose. A furni's logic reads this field, and feeding it a marker
                // string is how a benign-looking stamp turns into a parse error at room load. The
                // run's items are recognised by their owner instead -- a `__bench__` account -- which
                // survives a crash just as well and cannot corrupt anything.
                ExtraData = string.Empty,
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

    /// <summary>
    /// The room object ids of the items this run placed, so a synthetic player can move and use
    /// real furniture instead of only walking past it. Empty when the run asked for no furniture.
    /// </summary>
    /// <remarks>
    /// Only what the run placed, never what a borrowed room already held: a bench player shoving a
    /// real player's sofa across the floor would be a measurement that edits the hotel.
    /// </remarks>
    public required ImmutableArray<int> FurnitureIds { get; init; }

    /// <summary>
    /// The synthetic players' own ids, in ticket order. The messenger addresses a conversation by
    /// the receiver's player id — see <c>SendMsgMessageHandler</c>, which parses <c>ChatId</c> as
    /// one — so this is what lets bench players write to each other rather than into the void.
    /// </summary>
    public required ImmutableArray<int> PlayerIds { get; init; }

    /// <summary>
    /// Page and offer pairs the accounts can actually afford and are allowed to see: visible, no
    /// club requirement, and carrying at least one product. A purchase of anything else is refused
    /// before it reaches the code the run means to measure.
    /// </summary>
    public required ImmutableArray<(int PageId, int OfferId)> CatalogOffers { get; init; }

    /// <summary>
    /// The name of the room model the run chose, so a synthetic player can create a room of its own
    /// with a model the hotel will accept.
    /// </summary>
    public required string RoomModelName { get; init; }
}
