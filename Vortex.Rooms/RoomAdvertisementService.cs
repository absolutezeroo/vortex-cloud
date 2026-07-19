using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Rooms;

namespace Vortex.Rooms;

internal sealed class RoomAdvertisementService(IDbContextFactory<VortexDbContext> dbContextFactory)
    : IRoomAdvertisementService
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;

    public async Task<RoomAdvertisementSnapshot?> EditAsync(
        int advertisementId,
        int actorPlayerId,
        string name,
        string? description,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        RoomAdvertisementEntity? entity = await dbCtx
            .RoomAdvertisements.Include(a => a.RoomEntity)
            .FirstOrDefaultAsync(
                a => a.Id == advertisementId && a.ExpiresAt > now && a.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (entity is null || entity.RoomEntity?.PlayerEntityId != actorPlayerId)
        {
            return null;
        }

        entity.Name = name.Length > 100 ? name[..100] : name;
        entity.Description =
            description is not null && description.Length > 255 ? description[..255] : description;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return new RoomAdvertisementSnapshot
        {
            RoomId = entity.RoomEntityId,
            Name = entity.Name,
            Description = entity.Description,
            CategoryId = entity.CategoryId,
            ExpiresAt = entity.ExpiresAt,
        };
    }

    public async Task<RoomId?> CancelAsync(
        int advertisementId,
        int actorPlayerId,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        RoomAdvertisementEntity? entity = await dbCtx
            .RoomAdvertisements.Include(a => a.RoomEntity)
            .FirstOrDefaultAsync(
                a => a.Id == advertisementId && a.ExpiresAt > now && a.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (entity is null || entity.RoomEntity?.PlayerEntityId != actorPlayerId)
        {
            return null;
        }

        entity.ExpiresAt = now;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return entity.RoomEntityId;
    }

    public async Task<bool> HasActiveAdvertisementAsync(int roomId, CancellationToken ct = default)
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        return await dbCtx
            .RoomAdvertisements.AnyAsync(
                a => a.RoomEntityId == roomId && a.ExpiresAt > now && a.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);
    }

    public async Task CreateAsync(
        int roomId,
        string name,
        string? description,
        int categoryId,
        bool extended,
        DateTime expiresAt,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        dbCtx.RoomAdvertisements.Add(
            new RoomAdvertisementEntity
            {
                RoomEntityId = roomId,
                Name = name.Length > 100 ? name[..100] : name,
                Description =
                    description is not null && description.Length > 255
                        ? description[..255]
                        : description,
                CategoryId = categoryId,
                Extended = extended,
                ExpiresAt = expiresAt,
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
