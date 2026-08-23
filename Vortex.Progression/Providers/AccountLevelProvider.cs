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

            logger.LogInformation("Loaded {Count} account level(s).", _rungs.Length);
        }
        catch (Exception ex)
        {
            // A failed load leaves the previous ladder in place; the profile then shows the floor
            // level rather than a wrong one, and the failure is visible instead of silent.
            logger.LogError(ex, "Failed to load the account level ladder.");
        }
    }
}
