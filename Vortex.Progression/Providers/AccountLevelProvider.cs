using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Players.Providers;
using Vortex.Progression.Achievements;

namespace Vortex.Progression.Providers;

/// <summary>
/// Caches the account level ladder. It is reference data — a dozen rows read on every profile
/// open — so it loads once with the other reference caches rather than hitting the database each
/// time someone clicks an avatar.
/// </summary>
internal sealed class AccountLevelProvider(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ILogger<AccountLevelProvider> logger
) : IAccountLevelProvider, IReferenceDataProvider
{
    private ImmutableArray<(int Level, int RequiredScore)> _rungs = ImmutableArray<(
        int,
        int
    )>.Empty;

    /// <summary>
    /// Whether a load has ever succeeded — which is what makes "keep the previous ladder" a
    /// recovery rather than a way to come up empty. Not the same as <c>_rungs</c> being non-empty:
    /// a hotel may legitimately have no ladder rows, and that load succeeded.
    /// </summary>
    private bool _loaded;

    public int LoadStage => 0;

    public int ResolveLevel(int achievementScore) =>
        AccountLevelLadder.Resolve(_rungs, achievementScore);

    public async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext db = await dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            List<(int, int)> rungs =
            [
                .. (
                    await db
                        .AccountLevels.AsNoTracking()
                        .OrderBy(l => l.RequiredScore)
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).Select(l => (l.LevelNumber, l.RequiredScore)),
            ];

            _rungs = [.. rungs];
            _loaded = true;

            logger.LogInformation("Loaded {Count} account level(s).", _rungs.Length);
        }
        catch (Exception ex) when (_loaded)
        {
            // A failed RELOAD leaves the previous ladder in place. A failed FIRST load must not:
            // "keep what we had" is only a recovery when there is something to keep, and at startup
            // there is not — the hotel would come up serving the floor level to everyone, quietly.
            // The filter lets that one rethrow, where VortexEmulator turns it into a startup
            // failure somebody reads.
            logger.LogError(ex, "Failed to reload the account level ladder; keeping the previous.");
        }
    }
}
