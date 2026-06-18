using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Rooms;

namespace Turbo.Rooms;

/// <summary>
/// EF Core backed <see cref="IRoomModerationStore"/>. Upserts use tracked operations so a repeated ban/mute
/// on the same player extends the existing row instead of failing the unique (room, player) index. The
/// database is the source of truth; the room grain caches active mutes in memory for chat enforcement.
/// </summary>
internal sealed class RoomModerationStore(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    ILogger<RoomModerationStore> logger
) : IRoomModerationStore
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<RoomModerationStore> _logger = logger;

    public async Task<bool> IsBannedAsync(int roomId, int playerId, CancellationToken ct = default)
    {
        if (roomId <= 0 || playerId <= 0)
            return false;

        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        return await dbCtx
            .RoomBans.AsNoTracking()
            .AnyAsync(
                b =>
                    b.RoomEntityId == roomId
                    && b.PlayerEntityId == playerId
                    && b.DeletedAt == null
                    && b.DateExpires > now,
                ct
            )
            .ConfigureAwait(false);
    }

    public Task BanAsync(
        int roomId,
        int playerId,
        DateTime expiresUtc,
        CancellationToken ct = default
    ) => UpsertExpiryAsync(ModerationKind.Ban, roomId, playerId, expiresUtc, ct);

    public Task MuteAsync(
        int roomId,
        int playerId,
        DateTime expiresUtc,
        CancellationToken ct = default
    ) => UpsertExpiryAsync(ModerationKind.Mute, roomId, playerId, expiresUtc, ct);

    public async Task UnbanAsync(int roomId, int playerId, CancellationToken ct = default)
    {
        if (roomId <= 0 || playerId <= 0)
            return;

        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        try
        {
            RoomBanEntity? existing = await dbCtx
                .RoomBans.FirstOrDefaultAsync(
                    b =>
                        b.RoomEntityId == roomId
                        && b.PlayerEntityId == playerId
                        && b.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false);

            if (existing is null)
                return;

            existing.DeletedAt = DateTime.UtcNow;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to unban player {PlayerId} in room {RoomId}.",
                playerId,
                roomId
            );
        }
    }

    public async Task UnmuteAsync(int roomId, int playerId, CancellationToken ct = default)
    {
        if (roomId <= 0 || playerId <= 0)
            return;

        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        try
        {
            RoomMuteEntity? existing = await dbCtx
                .RoomMutes.FirstOrDefaultAsync(
                    m =>
                        m.RoomEntityId == roomId
                        && m.PlayerEntityId == playerId
                        && m.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false);

            if (existing is null)
                return;

            existing.DeletedAt = DateTime.UtcNow;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to unmute player {PlayerId} in room {RoomId}.",
                playerId,
                roomId
            );
        }
    }

    public async Task<IReadOnlyList<RoomMuteRecord>> GetActiveMutesAsync(
        int roomId,
        CancellationToken ct = default
    )
    {
        if (roomId <= 0)
            return [];

        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        return await dbCtx
            .RoomMutes.AsNoTracking()
            .Where(m =>
                m.RoomEntityId == roomId && m.DeletedAt == null && m.DateExpires > now
            )
            .Select(m => new RoomMuteRecord(m.PlayerEntityId, m.DateExpires))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task UpsertExpiryAsync(
        ModerationKind kind,
        int roomId,
        int playerId,
        DateTime expiresUtc,
        CancellationToken ct
    )
    {
        if (roomId <= 0 || playerId <= 0)
            return;

        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        try
        {
            if (kind == ModerationKind.Ban)
            {
                RoomBanEntity? existing = await dbCtx
                    .RoomBans.FirstOrDefaultAsync(
                        b => b.RoomEntityId == roomId && b.PlayerEntityId == playerId,
                        ct
                    )
                    .ConfigureAwait(false);

                if (existing is null)
                {
                    dbCtx.RoomBans.Add(
                        new RoomBanEntity
                        {
                            RoomEntityId = roomId,
                            PlayerEntityId = playerId,
                            DateExpires = expiresUtc,
                            RoomEntity = null!,
                            PlayerEntity = null!,
                        }
                    );
                }
                else
                {
                    existing.DateExpires = expiresUtc;
                    existing.DeletedAt = null;
                }
            }
            else
            {
                RoomMuteEntity? existing = await dbCtx
                    .RoomMutes.FirstOrDefaultAsync(
                        m => m.RoomEntityId == roomId && m.PlayerEntityId == playerId,
                        ct
                    )
                    .ConfigureAwait(false);

                if (existing is null)
                {
                    dbCtx.RoomMutes.Add(
                        new RoomMuteEntity
                        {
                            RoomEntityId = roomId,
                            PlayerEntityId = playerId,
                            DateExpires = expiresUtc,
                            RoomEntity = null!,
                            PlayerEntity = null!,
                        }
                    );
                }
                else
                {
                    existing.DateExpires = expiresUtc;
                    existing.DeletedAt = null;
                }
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist {Kind} for player {PlayerId} in room {RoomId}.",
                kind,
                playerId,
                roomId
            );
        }
    }

    private enum ModerationKind
    {
        Ban,
        Mute,
    }
}
