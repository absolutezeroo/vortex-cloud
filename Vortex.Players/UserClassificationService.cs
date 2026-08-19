using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Moderation;

namespace Vortex.Players;

/// <summary>
/// Resolves the staff <c>:uc</c> classifications. One query per call over an id set that is bounded
/// by the room's population (or the online count), never a scan: the caller always supplies the ids.
/// </summary>
internal sealed class UserClassificationService(IDbContextFactory<VortexDbContext> dbContextFactory)
    : IUserClassificationService
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;

    public async Task<ImmutableArray<UserClassificationEntry>> ClassifyAsync(
        IReadOnlyCollection<int> playerIds,
        string classification,
        int newUserWindowDays,
        CancellationToken ct = default
    )
    {
        if (playerIds.Count == 0 || string.IsNullOrWhiteSpace(classification))
        {
            return [];
        }

        string key = classification.Trim().ToLowerInvariant();

        return key switch
        {
            UserClassifications.New => await ClassifyNewAsync(playerIds, newUserWindowDays, ct)
                .ConfigureAwait(false),
            UserClassifications.Paying => await ClassifyPayingAsync(playerIds, ct)
                .ConfigureAwait(false),
            // Deliberately empty: the client sends whatever the moderator typed, and a label the
            // server does not know is a typo, not a request for everybody.
            _ => [],
        };
    }

    private async Task<ImmutableArray<UserClassificationEntry>> ClassifyNewAsync(
        IReadOnlyCollection<int> playerIds,
        int newUserWindowDays,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, newUserWindowDays));

        List<UserClassificationEntry> rows = await dbCtx
            .Players.AsNoTracking()
            .Where(p => playerIds.Contains(p.Id) && p.CreatedAt >= cutoff)
            .Select(p => new UserClassificationEntry(p.Id, p.Name, UserClassifications.New))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return [.. rows];
    }

    private async Task<ImmutableArray<UserClassificationEntry>> ClassifyPayingAsync(
        IReadOnlyCollection<int> playerIds,
        CancellationToken ct
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        List<UserClassificationEntry> rows = await dbCtx
            .PlayerSubscriptions.AsNoTracking()
            .Where(s =>
                playerIds.Contains(s.PlayerEntityId)
                && s.ExpiresAt > now
                && s.DeletedAt == null
                && s.PlayerEntity != null
            )
            .Select(s => new UserClassificationEntry(
                s.PlayerEntityId,
                s.PlayerEntity!.Name,
                UserClassifications.Paying
            ))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // A player can hold more than one subscription type; the list is per player, not per row.
        return [.. rows.DistinctBy(r => r.UserId)];
    }
}
