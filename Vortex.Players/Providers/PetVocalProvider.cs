using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Pets;
using Vortex.Primitives.Pets.Providers;

namespace Vortex.Players.Providers;

/// <summary>
/// Serves the lines pets say, loaded once from <c>pet_vocals</c>. Keyed by pet type and situation,
/// the same way the client's text bundle keys them.
/// </summary>
public sealed class PetVocalProvider(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<IPetVocalProvider> logger
) : IPetVocalProvider
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IPetVocalProvider> _logger = logger;

    private ImmutableDictionary<(int PetType, string VocalType), ImmutableArray<string>> _lines =
        ImmutableDictionary<(int, string), ImmutableArray<string>>.Empty;

    public string? TryGetLine(int petType, string vocalType)
    {
        if (!_lines.TryGetValue((petType, vocalType), out ImmutableArray<string> lines))
        {
            return null;
        }

        return lines.Length switch
        {
            0 => null,
            1 => lines[0],
            _ => lines[Random.Shared.Next(lines.Length)],
        };
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<PetVocalEntity> entities = await dbCtx
                .PetVocals.AsNoTracking()
                .Where(v => v.DeletedAt == null)
                .OrderBy(v => v.PetType)
                .ThenBy(v => v.VocalType)
                .ThenBy(v => v.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _lines = entities
                .GroupBy(e => (e.PetType, e.VocalType))
                .ToImmutableDictionary(g => g.Key, g => g.Select(e => e.Text).ToImmutableArray());

            _logger.LogInformation(
                "Loaded pet vocals: {Count} lines across {Buckets} type/situation pairs",
                entities.Count,
                _lines.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
