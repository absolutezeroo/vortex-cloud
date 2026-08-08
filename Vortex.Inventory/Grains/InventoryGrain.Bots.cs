using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Players;

namespace Vortex.Inventory.Grains;

public sealed partial class InventoryGrain
{
    public async Task<ImmutableArray<BotSnapshot>> GetAllBotSnapshotsAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            // Same filter as the pet inventory: a bot standing in a room is not in the hand, and a
            // soft-deleted one is gone.
            BotEntity[] entities = await dbCtx
                .Bots.AsNoTracking()
                .Where(b =>
                    b.OwnerPlayerEntityId == (int)this.GetPrimaryKeyLong()
                    && b.RoomEntityId == null
                    && b.DeletedAt == null
                )
                .OrderBy(b => b.Id)
                .ToArrayAsync(ct)
                .ConfigureAwait(true);

            return [.. entities.Select(ToSnapshot)];
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to list bot inventory for player {PlayerId}",
                this.GetPrimaryKeyLong()
            );
            throw;
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }
    }

    public async Task<BotSnapshot> CreateBotAsync(BotCreateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Bot name is required.", nameof(request));
        }

        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true);

        try
        {
            BotEntity entity = new()
            {
                OwnerPlayerEntityId = (int)this.GetPrimaryKeyLong(),
                RoomEntityId = null,
                Name = request.Name.Trim(),
                Motto = request.Motto,
                Figure = request.Figure,
                Gender = request.Gender,
            };

            dbCtx.Bots.Add(entity);

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return ToSnapshot(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create bot for player {PlayerId}",
                this.GetPrimaryKeyLong()
            );
            throw;
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(true);
        }
    }

    internal static BotSnapshot ToSnapshot(BotEntity entity) =>
        new()
        {
            BotId = entity.Id,
            OwnerId = (PlayerId)entity.OwnerPlayerEntityId,
            Name = entity.Name,
            Motto = entity.Motto,
            Figure = entity.Figure,
            Gender = entity.Gender,
        };
}
