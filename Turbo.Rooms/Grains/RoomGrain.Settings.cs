using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Orleans.Snapshots.Room.Settings;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<RoomSnapshot?> GetRoomSettingsAsync(PlayerId actor, CancellationToken ct)
    {
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return Task.FromResult<RoomSnapshot?>(null);
        }

        return Task.FromResult<RoomSnapshot?>(_state.RoomSnapshot);
    }

    public async Task<bool> UpdateRoomSettingsAsync(
        PlayerId actor,
        RoomSettingsUpdate update,
        CancellationToken ct
    )
    {
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return false;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return false;
            }

            entity.Name = update.Name;
            entity.Description = update.Description;
            entity.DoorMode = update.DoorMode;
            entity.Password = update.Password;
            entity.PlayersMax = Math.Clamp(update.MaxVisitors, 1, _roomConfig.MaxVisitorsLimit);
            entity.NavigatorCategoryEntityId = update.CategoryId > 0 ? update.CategoryId : null;
            entity.TradeType = update.TradeMode;
            entity.AllowPets = update.AllowPets;
            entity.AllowPetsEat = update.AllowPetsEat;
            entity.AllowBlocking = update.AllowBlocking;
            entity.HideWalls = update.HideWalls;
            entity.ThicknessWall = update.WallThickness;
            entity.ThicknessFloor = update.FloorThickness;
            entity.MuteType = update.WhoCanMute;
            entity.KickType = update.WhoCanKick;
            entity.BanType = update.WhoCanBan;
            entity.ChatModeType = update.ChatMode;
            entity.ChatBubbleType = update.ChatBubbleSize;
            entity.ChatSpeedType = update.ChatScrollSpeed;
            entity.ChatDistance = update.ChatFullHearRange;
            entity.ChatFloodType = update.ChatFloodSensitivity;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            _state.RoomSnapshot = _state.RoomSnapshot with
            {
                Name = entity.Name,
                Description = entity.Description ?? string.Empty,
                DoorMode = entity.DoorMode,
                Password = entity.Password ?? string.Empty,
                PlayersMax = entity.PlayersMax,
                CategoryId = entity.NavigatorCategoryEntityId ?? -1,
                TradeType = entity.TradeType,
                AllowPets = entity.AllowPets,
                AllowPetsEat = entity.AllowPetsEat,
                AllowBlocking = entity.AllowBlocking,
                HideWalls = entity.HideWalls,
                WallThickness = entity.ThicknessWall,
                FloorThickness = entity.ThicknessFloor,
                ModSettings = new ModSettingsSnapshot
                {
                    WhoCanMute = entity.MuteType,
                    WhoCanKick = entity.KickType,
                    WhoCanBan = entity.BanType,
                },
                ChatSettings = new ChatSettingsSnapshot
                {
                    ChatMode = entity.ChatModeType,
                    BubbleWidth = entity.ChatBubbleType,
                    ScrollSpeed = entity.ChatSpeedType,
                    FullHearRange = entity.ChatDistance,
                    FloodSensitivity = entity.ChatFloodType,
                },
                LastUpdatedUtc = DateTime.UtcNow,
            };

            await _grainFactory
                .GetRoomDirectoryGrain()
                .UpsertActiveRoomAsync(_state.RoomSnapshot)
                .ConfigureAwait(false);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new RoomSettingsSavedEventMessageComposer { RoomId = _state.RoomId }
                )
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings for room {RoomId}.", _state.RoomId);
            return false;
        }
    }

    public async Task<bool> DeleteRoomAsync(PlayerId actor, CancellationToken ct)
    {
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return false;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return false;
            }

            entity.DeletedAt = DateTime.UtcNow;
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            ActionContext actorCtx = ActionContext.CreateForPlayer(actor, _state.RoomId);

            List<PlayerId> occupants = _state.AvatarsByPlayerId.Keys.ToList();
            foreach (PlayerId playerId in occupants)
            {
                await AvatarModule
                    .RemoveAvatarFromPlayerAsync(actorCtx, playerId, ct)
                    .ConfigureAwait(false);
            }

            await _grainFactory.GetRoomDirectoryGrain().RemoveActiveRoomAsync(_state.RoomId);

            await DeactivateRoomAsync().ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete room {RoomId}.", _state.RoomId);
            return false;
        }
    }

    public async Task<ImmutableArray<RoomControllerSnapshot>> GetControllersAsync(
        CancellationToken ct
    )
    {
        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            List<RoomControllerSnapshot> result = await dbCtx
                .RoomRights.AsNoTracking()
                .Include(r => r.PlayerEntity)
                .Where(r => r.RoomEntityId == _state.RoomId.Value && r.DeletedAt == null)
                .Select(r => new RoomControllerSnapshot
                {
                    PlayerId = r.PlayerEntityId,
                    Name = r.PlayerEntity.Name,
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return [.. result];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load controllers for room {RoomId}.", _state.RoomId);
            return [];
        }
    }

    public async Task<ImmutableArray<RoomControllerSnapshot>> GetBannedUsersAsync(
        CancellationToken ct
    )
    {
        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            DateTime now = DateTime.UtcNow;

            List<RoomControllerSnapshot> result = await dbCtx
                .RoomBans.AsNoTracking()
                .Include(b => b.PlayerEntity)
                .Where(b =>
                    b.RoomEntityId == _state.RoomId.Value
                    && b.DeletedAt == null
                    && b.DateExpires > now
                )
                .Select(b => new RoomControllerSnapshot
                {
                    PlayerId = b.PlayerEntityId,
                    Name = b.PlayerEntity.Name,
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return [.. result];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load banned users for room {RoomId}.", _state.RoomId);
            return [];
        }
    }

    public async Task<bool> UpdateCategoryAndTradeAsync(
        PlayerId actor,
        int categoryId,
        RoomTradeModeType tradeType,
        CancellationToken ct
    )
    {
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return false;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return false;
            }

            entity.NavigatorCategoryEntityId = categoryId > 0 ? categoryId : null;
            entity.TradeType = tradeType;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            _state.RoomSnapshot = _state.RoomSnapshot with
            {
                CategoryId = entity.NavigatorCategoryEntityId ?? -1,
                TradeType = entity.TradeType,
                LastUpdatedUtc = DateTime.UtcNow,
            };

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new RoomSettingsSavedEventMessageComposer { RoomId = _state.RoomId }
                )
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update category and trade for room {RoomId}.",
                _state.RoomId
            );
            return false;
        }
    }

    public async Task AssignRightsAsync(PlayerId actor, PlayerId target, CancellationToken ct)
    {
        if (_state.RoomSnapshot.OwnerId != actor || target <= 0 || actor == target)
        {
            return;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            bool alreadyHasRights = await dbCtx
                .RoomRights.AnyAsync(
                    r =>
                        r.RoomEntityId == _state.RoomId.Value
                        && r.PlayerEntityId == target.Value
                        && r.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false);

            if (alreadyHasRights)
            {
                return;
            }

            dbCtx.RoomRights.Add(
                new RoomRightEntity
                {
                    RoomEntityId = _state.RoomId.Value,
                    PlayerEntityId = target.Value,
                    RoomEntity = null!,
                    PlayerEntity = null!,
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            ImmutableArray<RoomControllerSnapshot> controllers = await GetControllersAsync(ct)
                .ConfigureAwait(false);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = controllers,
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to assign rights to player {Target} in room {RoomId}.",
                target,
                _state.RoomId
            );
        }
    }

    public async Task RemoveRightsAsync(
        PlayerId actor,
        ImmutableArray<PlayerId> targets,
        CancellationToken ct
    )
    {
        if (_state.RoomSnapshot.OwnerId != actor || targets.IsEmpty)
        {
            return;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            int[] targetValues = [.. targets.Select(t => t.Value)];

            List<RoomRightEntity> rows = await dbCtx
                .RoomRights.Where(r =>
                    r.RoomEntityId == _state.RoomId.Value
                    && targetValues.Contains(r.PlayerEntityId)
                    && r.DeletedAt == null
                )
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (RoomRightEntity row in rows)
            {
                dbCtx.RoomRights.Remove(row);
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            ImmutableArray<RoomControllerSnapshot> controllers = await GetControllersAsync(ct)
                .ConfigureAwait(false);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = controllers,
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove rights in room {RoomId}.", _state.RoomId);
        }
    }

    public async Task RemoveAllRightsAsync(PlayerId actor, CancellationToken ct)
    {
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            List<RoomRightEntity> rows = await dbCtx
                .RoomRights.Where(r => r.RoomEntityId == _state.RoomId.Value && r.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (RoomRightEntity row in rows)
            {
                dbCtx.RoomRights.Remove(row);
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = [],
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all rights in room {RoomId}.", _state.RoomId);
        }
    }

    public async Task RemoveOwnRightsAsync(PlayerId actor, CancellationToken ct)
    {
        if (actor <= 0 || _state.RoomSnapshot.OwnerId == actor)
        {
            return;
        }

        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            RoomRightEntity? row = await dbCtx
                .RoomRights.FirstOrDefaultAsync(
                    r =>
                        r.RoomEntityId == _state.RoomId.Value
                        && r.PlayerEntityId == actor.Value
                        && r.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false);

            if (row is null)
            {
                return;
            }

            dbCtx.RoomRights.Remove(row);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            ImmutableArray<RoomControllerSnapshot> controllers = await GetControllersAsync(ct)
                .ConfigureAwait(false);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = controllers,
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to remove own rights for player {Actor} in room {RoomId}.",
                actor,
                _state.RoomId
            );
        }
    }
}
