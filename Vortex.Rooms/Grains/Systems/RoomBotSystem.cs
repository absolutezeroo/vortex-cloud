using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
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
public sealed class RoomBotSystem(RoomGrain roomGrain)
{
    /// <summary>
    /// Keeps bot object ids clear of both furniture and pets, which take the million above this one.
    /// Two occupants sharing an object id would have the client drawing one over the other.
    /// </summary>
    private const int BotRoomObjectIdOffset = 2_000_000;

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
                new UsersMessageComposer { Avatars = [ToAvatarSnapshot(snapshot)] }
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

        return [.. _botsById.Values.OrderBy(b => b.BotId).Select(ToAvatarSnapshot)];
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

    private static RoomBotAvatarSnapshot ToAvatarSnapshot(BotSnapshot bot) =>
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
            OwnerName = string.Empty,
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
