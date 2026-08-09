using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Messages.Outgoing.Room.Bots;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Bots standing in the room. Deliberately shaped like <see cref="RoomPetSystem"/> — same load on
/// first use, same "(0,0) means anywhere that works" drop rule, same avatar broadcast — because a
/// bot is the same kind of thing to a room: an owned occupant that is not a player.
/// <para>
/// Unlike a pet it has no needs, no tick and nothing to flush periodically, so placement writes
/// straight through to the database rather than riding a dirty-set.
/// </para>
/// </summary>
public sealed partial class RoomBotSystem(RoomGrain roomGrain)
{
    /// <summary>
    /// Keeps bot object ids clear of both furniture and pets, which take the million above this one.
    /// Two occupants sharing an object id would have the client drawing one over the other.
    /// </summary>
    private const int BotRoomObjectIdOffset = 2_000_000;

    /// <summary>Bots only speak, so this ticks far slower than the avatar or pet clocks.</summary>
    private const int BotTickMs = 1_000;

    private const int ChatterMinIntervalMs = 12_000;
    private const int ChatterMaxIntervalMs = 40_000;

    private readonly RoomGrain _roomGrain = roomGrain;

    private readonly Dictionary<int, BotSnapshot> _botsById = [];

    private bool _loaded;

    public static RoomObjectId ToRoomObjectId(int botId) => botId + BotRoomObjectIdOffset;

    public async Task EnsureBotsLoadedAsync(CancellationToken ct)
    {
        if (_loaded)
        {
            return;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity[] bots = await dbCtx
            .Bots.AsNoTracking()
            .Where(b => b.RoomEntityId == _roomGrain.RoomId.Value && b.DeletedAt == null)
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        _botsById.Clear();

        foreach (BotEntity bot in bots)
        {
            _botsById[bot.Id] = ToSnapshot(bot);
        }

        _loaded = true;
    }

    public async Task<BotSnapshot?> PlaceBotAsync(
        ActionContext ctx,
        int botId,
        int x,
        int y,
        CancellationToken ct
    )
    {
        if (!await _roomGrain.SecurityModule.CanPlaceFurniAsync(ctx).ConfigureAwait(true))
        {
            return null;
        }

        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        // (0, 0) from the client means "dropped from the inventory, put it somewhere" rather than
        // the corner tile — the same convention pets use, and reading it literally would reject the
        // drop in every room whose corner is blocked.
        if (!TryResolveDropTile(x, y, out int resolvedX, out int resolvedY))
        {
            return null;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity? bot = await dbCtx
            .Bots.SingleOrDefaultAsync(
                b =>
                    b.Id == botId
                    && b.OwnerPlayerEntityId == ctx.PlayerId.Value
                    && b.DeletedAt == null,
                ct
            )
            .ConfigureAwait(true);

        if (bot is null)
        {
            _roomGrain._logger.LogDebug(
                "Bot placement ignored because bot {BotId} was not found for player {PlayerId}",
                botId,
                ctx.PlayerId
            );

            return null;
        }

        if (bot.RoomEntityId is not null)
        {
            _roomGrain._logger.LogDebug(
                "Bot placement ignored because bot {BotId} is already in room {ExistingRoomId}",
                botId,
                bot.RoomEntityId
            );

            return null;
        }

        bot.RoomEntityId = _roomGrain.RoomId.Value;
        bot.X = resolvedX;
        bot.Y = resolvedY;
        bot.Z = _roomGrain
            ._state.TileHeights[_roomGrain.MapModule.ToIdx(resolvedX, resolvedY)]
            .ToInt();
        bot.Rotation = Rotation.South;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        BotSnapshot snapshot = ToSnapshot(bot);
        _botsById[bot.Id] = snapshot;

        // The bot left the hand, so the owner's inventory has to lose the row or it will show a bot
        // that is standing in front of them.
        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(snapshot.OwnerId)
            .SendComposerAsync(
                new BotRemovedFromInventoryEventMessageComposer { BotId = snapshot.BotId }
            )
            .ConfigureAwait(true);

        await _roomGrain
            .SendComposerToRoomAsync(
                new UsersMessageComposer
                {
                    Avatars =
                    [
                        ToAvatarSnapshot(
                            snapshot,
                            await GetOwnerNameAsync(snapshot.OwnerId, ct).ConfigureAwait(true)
                        ),
                    ],
                }
            )
            .ConfigureAwait(true);

        return snapshot;
    }

    public async Task<bool> RemoveBotAsync(ActionContext ctx, int botId, CancellationToken ct)
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.TryGetValue(botId, out BotSnapshot? placed))
        {
            return false;
        }

        // Rights rather than ownership, matching how furniture pickup works: whoever may build here
        // may clear the room, and a bot left behind by a visitor is the owner's problem otherwise.
        bool isOwner = placed.OwnerId == ctx.PlayerId;

        if (
            !isOwner
            && !await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx).ConfigureAwait(true)
        )
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity? bot = await dbCtx
            .Bots.SingleOrDefaultAsync(
                b => b.Id == botId && b.RoomEntityId == _roomGrain.RoomId.Value,
                ct
            )
            .ConfigureAwait(true);

        if (bot is null)
        {
            // The row moved out from under the live state; drop it rather than keep drawing a bot
            // the database no longer places here.
            _botsById.Remove(botId);

            return false;
        }

        bot.RoomEntityId = null;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        _botsById.Remove(botId);
        InvalidateChatter(botId);

        await _roomGrain
            .SendComposerToRoomAsync(
                new UserRemoveMessageComposer { ObjectId = ToRoomObjectId(botId) }
            )
            .ConfigureAwait(true);

        // Back to the hand it came from — its owner's, not the remover's.
        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(placed.OwnerId)
            .SendComposerAsync(
                new BotAddedToInventoryEventMessageComposer
                {
                    Bot = placed with { X = 0, Y = 0, Z = default, Rotation = Rotation.North },
                    OpenInventory = isOwner,
                }
            )
            .ConfigureAwait(true);

        return true;
    }

    public async Task<ImmutableArray<RoomAvatarSnapshot>> GetPlacedBotAvatarSnapshotsAsync(
        CancellationToken ct
    )
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        ImmutableArray<RoomAvatarSnapshot>.Builder avatars =
            ImmutableArray.CreateBuilder<RoomAvatarSnapshot>(_botsById.Count);

        foreach (BotSnapshot bot in _botsById.Values.OrderBy(b => b.BotId))
        {
            avatars.Add(
                ToAvatarSnapshot(bot, await GetOwnerNameAsync(bot.OwnerId, ct).ConfigureAwait(true))
            );
        }

        return avatars.ToImmutable();
    }

    /// <summary>
    /// The bot's owner name, which the client shows on its menu. Cached on the room alongside the
    /// pets' — a bot standing in a room would otherwise cost a directory lookup on every redraw.
    /// </summary>
    private async Task<string> GetOwnerNameAsync(PlayerId ownerId, CancellationToken ct)
    {
        if (_roomGrain._state.OwnerNamesById.TryGetValue(ownerId, out string? cached))
        {
            return cached;
        }

        string ownerName = await _roomGrain
            ._grainFactory.GetPlayerDirectoryGrain()
            .GetPlayerNameAsync(ownerId, ct)
            .ConfigureAwait(true);

        _roomGrain._state.OwnerNamesById[ownerId] = ownerName;

        return ownerName;
    }

    public async Task<bool> SetBotSkillAsync(
        ActionContext ctx,
        int botId,
        int commandId,
        string data,
        CancellationToken ct
    )
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.TryGetValue(botId, out BotSnapshot? placed))
        {
            return false;
        }

        // Only the bot's owner configures it. Room rights let a visitor's bot be cleared away, not
        // reprogrammed — someone else's bot saying what you typed is a different problem entirely.
        if (placed.OwnerId != ctx.PlayerId)
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity? bot = await dbCtx
            .Bots.SingleOrDefaultAsync(
                b => b.Id == botId && b.RoomEntityId == _roomGrain.RoomId.Value,
                ct
            )
            .ConfigureAwait(true);

        if (bot is null)
        {
            return false;
        }

        Dictionary<string, string> skills = ReadSkills(bot);
        skills[commandId.ToString(CultureInfo.InvariantCulture)] = data;

        bot.SkillsJson = JsonSerializer.Serialize(skills);

        // The tick reads a cached phrase list; without this the bot keeps saying the old lines.
        InvalidateChatter(botId);

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new BotSkillListUpdateMessageComposer
                {
                    BotId = botId,
                    Skills =
                    [
                        .. skills.Select(pair => new BotSkillEntry
                        {
                            CommandId = int.Parse(pair.Key, CultureInfo.InvariantCulture),
                            Data = pair.Value,
                        }),
                    ],
                }
            )
            .ConfigureAwait(true);

        return true;
    }

    public async Task<string?> GetBotSkillAsync(int botId, int commandId, CancellationToken ct)
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.ContainsKey(botId))
        {
            return null;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity? bot = await dbCtx
            .Bots.AsNoTracking()
            .SingleOrDefaultAsync(
                b => b.Id == botId && b.RoomEntityId == _roomGrain.RoomId.Value,
                ct
            )
            .ConfigureAwait(true);

        if (bot is null)
        {
            return null;
        }

        // An unconfigured skill is empty, not absent: the dialog opens either way and wants
        // something to render.
        return ReadSkills(bot)
            .GetValueOrDefault(commandId.ToString(CultureInfo.InvariantCulture), string.Empty);
    }

    /// <summary>
    /// Tolerates a malformed blob rather than throwing: the column is free text as far as the
    /// database is concerned, and losing one bot's settings is better than failing every read.
    /// </summary>
    private Dictionary<string, string> ReadSkills(BotEntity bot)
    {
        if (string.IsNullOrWhiteSpace(bot.SkillsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(bot.SkillsJson) ?? [];
        }
        catch (JsonException ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Bot {BotId} in room {RoomId} has unreadable skills_json; treating it as unconfigured.",
                bot.Id,
                _roomGrain._state.RoomId.Value
            );

            return [];
        }
    }

    private bool TryResolveDropTile(int x, int y, out int resolvedX, out int resolvedY) =>
        RoomPetRuntime.TryResolveDropTile(
            x,
            y,
            _roomGrain._state.Model?.DoorX ?? 0,
            _roomGrain._state.Model?.DoorY ?? 0,
            _roomGrain.MapModule.Width,
            _roomGrain.MapModule.Height,
            IsTileFreeForBot,
            out resolvedX,
            out resolvedY
        );

    private bool IsTileFreeForBot(int x, int y)
    {
        if (!_roomGrain.MapModule.InBounds(x, y))
        {
            return false;
        }

        RoomTileFlags flags = _roomGrain._state.TileFlags[_roomGrain.MapModule.ToIdx(x, y)];

        return !flags.Has(RoomTileFlags.Disabled)
            && !flags.Has(RoomTileFlags.Closed)
            && !flags.Has(RoomTileFlags.AvatarOccupied);
    }

    private static RoomBotAvatarSnapshot ToAvatarSnapshot(BotSnapshot bot, string ownerName = "") =>
        new()
        {
            AvatarType = RoomObjectType.Bot,
            WebId = bot.BotId,
            Name = bot.Name,
            Motto = bot.Motto,
            Figure = bot.Figure,
            ObjectId = ToRoomObjectId(bot.BotId),
            X = bot.X,
            Y = bot.Y,
            Z = bot.Z,
            BodyRotation = bot.Rotation,
            HeadRotation = bot.Rotation,
            Status = "/",
            Gender = bot.Gender,
            OwnerId = bot.OwnerId.Value,
            OwnerName = ownerName,
        };

    private static BotSnapshot ToSnapshot(BotEntity entity) =>
        new()
        {
            BotId = entity.Id,
            OwnerId = (PlayerId)entity.OwnerPlayerEntityId,
            Name = entity.Name,
            Motto = entity.Motto,
            Figure = entity.Figure,
            Gender = entity.Gender,
            X = entity.X,
            Y = entity.Y,
            Z = Altitude.FromInt(entity.Z),
            Rotation = entity.Rotation,
        };
}
