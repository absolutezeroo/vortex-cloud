using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Rooms;

namespace Turbo.Rooms;

internal sealed class RoomAdvertisementService(IDbContextFactory<TurboDbContext> dbContextFactory)
    : IRoomAdvertisementService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;

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
        await using TurboDbContext dbCtx = await _dbContextFactory
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
