using System;
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
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Orleans.Snapshots.Room.Settings;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Outgoing.Room.Chat;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    /// <summary>
    /// "May this subject administer the room?" — explicit ownership OR the staff build capability,
    /// as decided by <see cref="Primitives.Permissions.RoomSecurityPolicy.IsRoomOwner"/>. Room
    /// administration used to compare the owner id directly, which silently locked out staff
    /// holding <c>room.build.any</c> and even wildcard administrators.
    /// </summary>
    private Task<bool> IsRoomOwnerAsync(PlayerId actor) =>
        SecurityModule.GetIsRoomOwnerAsync(
            new ActionContext { PlayerId = actor, Origin = ActionOrigin.Player }
        );

    public async Task<RoomSnapshot?> GetRoomSettingsAsync(PlayerId actor, CancellationToken ct)
    {
        if (!await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return null;
        }

        return _state.RoomSnapshot;
    }

    public async Task<bool> UpdateRoomSettingsAsync(
        PlayerId actor,
        RoomSettingsUpdate update,
        CancellationToken ct
    )
    {
        if (!await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(true);

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
            entity.ChatFloodType = update.ChatFloodSensitivity;
            entity.LeaveOnDoorTile = update.LeaveOnDoorTile;
            entity.IdleSleepEnabled = update.IdleSleepEnabled;
            entity.IdleSleepTimeoutSeconds = update.IdleSleepTimeoutSeconds;
            entity.IdleAutokickEnabled = update.IdleAutokickEnabled;
            entity.IdleAutokickTimeoutSeconds = update.IdleAutokickTimeoutSeconds;
            entity.MuteAllPets = update.MuteAllPets;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

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
                LeaveOnDoorTile = entity.LeaveOnDoorTile,
                IdleSleepEnabled = entity.IdleSleepEnabled,
                IdleSleepTimeoutSeconds = entity.IdleSleepTimeoutSeconds,
                IdleAutokickEnabled = entity.IdleAutokickEnabled,
                IdleAutokickTimeoutSeconds = entity.IdleAutokickTimeoutSeconds,
                MuteAllPets = entity.MuteAllPets,
                LastUpdatedUtc = DateTime.UtcNow,
            };

            using (
                _metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.UpsertActiveRoomAsync))
            )
            {
                await _grainFactory
                    .GetRoomDirectoryGrain()
                    .UpsertActiveRoomAsync(_state.RoomSnapshot)
                    .ConfigureAwait(true);
            }

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new RoomSettingsSavedEventMessageComposer { RoomId = _state.RoomId }
                )
                .ConfigureAwait(true);

            // Everyone standing in the room, not just the owner who saved: flood sensitivity is
            // what the client throttles its own chat against, and it otherwise only arrives with
            // GuestRoomData -- so a loosened setting used to take effect for the people already
            // inside only once they left and came back.
            await SendComposerToRoomAsync(
                    new RoomChatSettingsMessageComposer { FloodSensitivity = entity.ChatFloodType }
                )
                .ConfigureAwait(true);

            // Same reasoning, and the same bug one row over: hiding the walls or changing either
            // thickness only ever reached a client on room entry, because this was the one place
            // that changes them and it never said so. The client has handled a mid-session update
            // all along -- RoomMessageHandler.onRoomVisualizationSettings() calls
            // updateObjectRoomVisibilities() and updateObjectRoomPlaneThicknesses() straight away --
            // so the walls came down for whoever saved only after they left and came back.
            await SendComposerToRoomAsync(
                    new RoomVisualizationSettingsMessageComposer
                    {
                        WallsHidden = entity.HideWalls,
                        WallThickness = entity.ThicknessWall,
                        FloorThickness = entity.ThicknessFloor,
                    }
                )
                .ConfigureAwait(true);

            await _events
                .PublishAsync(
                    new RoomSettingsUpdatedEvent(
                        actor,
                        _state.RoomId.Value,
                        _state.RoomSnapshot.Name,
                        "settings"
                    ),
                    ct
                )
                .ConfigureAwait(true);

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
        // Deliberately the literal owner only: deleting somebody's room is not part of
        // room.build.any, and staff removals go through the moderation tooling instead.
        if (_state.RoomSnapshot.OwnerId != actor)
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(true);

            if (entity is null)
            {
                return false;
            }

            entity.DeletedAt = DateTime.UtcNow;
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            ActionContext actorCtx = ActionContext.CreateForPlayer(actor, _state.RoomId);

            List<PlayerId> occupants = _state.AvatarsByPlayerId.Keys.ToList();
            foreach (PlayerId playerId in occupants)
            {
                await AvatarModule
                    .RemoveAvatarFromPlayerAsync(actorCtx, playerId, ct)
                    .ConfigureAwait(true);
            }

            using (
                _metrics.MeasureRoomDirectoryCall(nameof(IRoomDirectoryGrain.RemoveActiveRoomAsync))
            )
            {
                await _grainFactory.GetRoomDirectoryGrain().RemoveActiveRoomAsync(_state.RoomId);
            }

            await _events
                .PublishAsync(
                    new RoomDeletedEvent(actor, _state.RoomId.Value, _state.RoomSnapshot.Name),
                    ct
                )
                .ConfigureAwait(true);

            await DeactivateRoomAsync().ConfigureAwait(true);

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
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<RoomControllerSnapshot> result = await dbCtx
                .RoomRights.AsNoTracking()
                .Include(r => r.PlayerEntity)
                .Where(r => r.RoomEntityId == _state.RoomId.Value && r.DeletedAt == null)
                .Select(r => new RoomControllerSnapshot
                {
                    PlayerId = r.PlayerEntityId,
                    Name = r.PlayerEntity != null ? r.PlayerEntity.Name : string.Empty,
                })
                .ToListAsync(ct)
                .ConfigureAwait(true);

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
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

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
                .ConfigureAwait(true);

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
        if (!await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(true);

            if (entity is null)
            {
                return false;
            }

            entity.NavigatorCategoryEntityId = categoryId > 0 ? categoryId : null;
            entity.TradeType = tradeType;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

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
                .ConfigureAwait(true);

            await _events
                .PublishAsync(
                    new RoomSettingsUpdatedEvent(
                        actor,
                        _state.RoomId.Value,
                        _state.RoomSnapshot.Name,
                        "category_trade"
                    ),
                    ct
                )
                .ConfigureAwait(true);

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
        if (target <= 0 || actor == target || !await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            bool alreadyHasRights = await dbCtx
                .RoomRights.AnyAsync(
                    r =>
                        r.RoomEntityId == _state.RoomId.Value
                        && r.PlayerEntityId == target.Value
                        && r.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (alreadyHasRights)
            {
                return;
            }

            dbCtx.RoomRights.Add(
                new RoomRightEntity
                {
                    RoomEntityId = _state.RoomId.Value,
                    PlayerEntityId = target.Value,
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            _state.PlayerIdsWithRights.Add(target);

            ImmutableArray<RoomControllerSnapshot> controllers = await GetControllersAsync(ct)
                .ConfigureAwait(true);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = controllers,
                    }
                )
                .ConfigureAwait(true);

            await SecurityModule
                .RefreshControllerLevelForPlayerAsync(target, ct)
                .ConfigureAwait(true);

            await _events
                .PublishAsync(
                    new RoomRightsChangedEvent(actor, _state.RoomId.Value, target, "granted"),
                    ct
                )
                .ConfigureAwait(true);
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
        if (targets.IsEmpty || !await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            int[] targetValues = [.. targets.Select(t => t.Value)];

            List<RoomRightEntity> rows = await dbCtx
                .RoomRights.Where(r =>
                    r.RoomEntityId == _state.RoomId.Value
                    && targetValues.AsEnumerable().Contains(r.PlayerEntityId)
                    && r.DeletedAt == null
                )
                .ToListAsync(ct)
                .ConfigureAwait(true);

            foreach (RoomRightEntity row in rows)
            {
                dbCtx.RoomRights.Remove(row);
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            foreach (PlayerId target in targets)
            {
                _state.PlayerIdsWithRights.Remove(target);
            }

            ImmutableArray<RoomControllerSnapshot> controllers = await GetControllersAsync(ct)
                .ConfigureAwait(true);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = controllers,
                    }
                )
                .ConfigureAwait(true);

            await Task.WhenAll(
                    targets.Select(target =>
                        SecurityModule.RefreshControllerLevelForPlayerAsync(target, ct)
                    )
                )
                .ConfigureAwait(true);

            // One record per target rather than one for the batch: an investigation searches by the
            // account that lost the rights at least as often as by the owner who took them away.
            foreach (PlayerId target in targets)
            {
                await _events
                    .PublishAsync(
                        new RoomRightsChangedEvent(actor, _state.RoomId.Value, target, "removed"),
                        ct
                    )
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove rights in room {RoomId}.", _state.RoomId);
        }
    }

    public async Task RemoveAllRightsAsync(PlayerId actor, CancellationToken ct)
    {
        if (!await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<RoomRightEntity> rows = await dbCtx
                .RoomRights.Where(r => r.RoomEntityId == _state.RoomId.Value && r.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<PlayerId> targets = [.. rows.Select(r => (PlayerId)r.PlayerEntityId)];

            foreach (RoomRightEntity row in rows)
            {
                dbCtx.RoomRights.Remove(row);
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            foreach (PlayerId target in targets)
            {
                _state.PlayerIdsWithRights.Remove(target);
            }

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = [],
                    }
                )
                .ConfigureAwait(true);

            await Task.WhenAll(
                    targets.Select(target =>
                        SecurityModule.RefreshControllerLevelForPlayerAsync(target, ct)
                    )
                )
                .ConfigureAwait(true);

            // No target: "cleared the room's rights" is one act, and the individual losses are not
            // separate decisions the way a targeted removal is.
            await _events
                .PublishAsync(
                    new RoomRightsChangedEvent(actor, _state.RoomId.Value, null, "removed_all"),
                    ct
                )
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all rights in room {RoomId}.", _state.RoomId);
        }
    }

    public async Task<bool> SetRoomTagsAsync(
        PlayerId actor,
        string? tag1,
        string? tag2,
        CancellationToken ct
    )
    {
        if (!await IsRoomOwnerAsync(actor).ConfigureAwait(true))
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(true);

            if (entity is null)
            {
                return false;
            }

            static string? Normalize(string? tag)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return null;
                }

                string trimmed = tag.Trim();
                return trimmed.Length > 25 ? trimmed[..25] : trimmed;
            }

            entity.Tag1 = Normalize(tag1);
            entity.Tag2 = Normalize(tag2);

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            _state.RoomSnapshot = _state.RoomSnapshot with
            {
                Tags = RoomTagMapper.ToTags(entity.Tag1, entity.Tag2),
                LastUpdatedUtc = DateTime.UtcNow,
            };

            await _events
                .PublishAsync(
                    new RoomSettingsUpdatedEvent(
                        actor,
                        _state.RoomId.Value,
                        _state.RoomSnapshot.Name,
                        "tags"
                    ),
                    ct
                )
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set tags for room {RoomId}.", _state.RoomId);
            return false;
        }
    }

    public async Task<bool> RateRoomAsync(PlayerId actor, int points, CancellationToken ct)
    {
        int sign = Math.Sign(points);

        if (sign == 0 || _state.RoomSnapshot.OwnerId == actor)
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            bool alreadyRated = await dbCtx
                .RoomRatings.AnyAsync(
                    r => r.RoomEntityId == _state.RoomId.Value && r.PlayerEntityId == actor.Value,
                    ct
                )
                .ConfigureAwait(true);

            if (alreadyRated)
            {
                return false;
            }

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(true);

            if (entity is null)
            {
                return false;
            }

            dbCtx.RoomRatings.Add(
                new RoomRatingEntity
                {
                    RoomEntityId = _state.RoomId.Value,
                    PlayerEntityId = actor.Value,
                    RoomEntity = null!,
                    PlayerEntity = null!,
                }
            );

            entity.Score += sign;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            _state.RoomSnapshot = _state.RoomSnapshot with
            {
                Score = entity.Score,
                LastUpdatedUtc = DateTime.UtcNow,
            };

            await _events
                .PublishAsync(new RoomRatedEvent(actor, _state.RoomId.Value, sign), ct)
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate room {RoomId}.", _state.RoomId);
            return false;
        }
    }

    public async Task SetStaffPickAsync(PlayerId actor, bool staffPick, CancellationToken ct)
    {
        // Same shape as ApplyStaffRoomActionsAsync: a staff power with no in-room level behind it,
        // checked until now only by the packet handler although the method is a member of a public
        // grain interface (ROOMG-GATE-038). The actor is a parameter for exactly this reason.
        if (
            !await SecurityModule
                .HasCapabilityAsync(actor, Capabilities.Navigator.StaffPick)
                .ConfigureAwait(true)
        )
        {
            _logger.LogWarning(
                "Player {Actor} tried to set the staff pick on room {RoomId} without {Capability}.",
                actor,
                _state.RoomId,
                Capabilities.Navigator.StaffPick
            );

            return;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomEntity? entity = await dbCtx
                .Rooms.FirstOrDefaultAsync(r => r.Id == _state.RoomId.Value, ct)
                .ConfigureAwait(true);

            if (entity is null)
            {
                return;
            }

            entity.IsStaffPick = staffPick;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            _state.RoomSnapshot = _state.RoomSnapshot with
            {
                StaffPick = staffPick,
                LastUpdatedUtc = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set staff pick for room {RoomId}.", _state.RoomId);
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
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomRightEntity? row = await dbCtx
                .RoomRights.FirstOrDefaultAsync(
                    r =>
                        r.RoomEntityId == _state.RoomId.Value
                        && r.PlayerEntityId == actor.Value
                        && r.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                return;
            }

            dbCtx.RoomRights.Remove(row);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            _state.PlayerIdsWithRights.Remove(actor);

            ImmutableArray<RoomControllerSnapshot> controllers = await GetControllersAsync(ct)
                .ConfigureAwait(true);

            await _grainFactory
                .GetPlayerPresenceGrain(actor)
                .SendComposerAsync(
                    new FlatControllersEventMessageComposer
                    {
                        RoomId = _state.RoomId,
                        Controllers = controllers,
                    }
                )
                .ConfigureAwait(true);

            await SecurityModule
                .RefreshControllerLevelForPlayerAsync(actor, ct)
                .ConfigureAwait(true);

            await _events
                .PublishAsync(
                    new RoomRightsChangedEvent(actor, _state.RoomId.Value, actor, "gave_up"),
                    ct
                )
                .ConfigureAwait(true);
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
