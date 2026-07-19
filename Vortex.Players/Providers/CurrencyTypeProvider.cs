using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Players.Providers;

public sealed class CurrencyTypeProvider(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<ICurrencyTypeProvider> logger
) : ICurrencyTypeProvider
{
    private sealed record State(
        ImmutableDictionary<int, CurrencyTypeSnapshot> ById,
        ImmutableDictionary<CurrencyKind, int> IdByKind
    );

    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<ICurrencyTypeProvider> _logger = logger;

    private State _state = new(
        ImmutableDictionary<int, CurrencyTypeSnapshot>.Empty,
        ImmutableDictionary<CurrencyKind, int>.Empty
    );

    public CurrencyTypeSnapshot? GetCurrencyType(int typeId)
    {
        return _state.ById.TryGetValue(typeId, out CurrencyTypeSnapshot? snapshot)
            ? snapshot
            : null;
    }

    public CurrencyTypeSnapshot? GetCurrencyTypeByKind(CurrencyKind kind)
    {
        State state = _state;

        return
            state.IdByKind.TryGetValue(kind, out int id)
            && state.ById.TryGetValue(id, out CurrencyTypeSnapshot? snapshot)
            ? snapshot
            : null;
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<CurrencyTypeEntity> entities = await dbCtx
                .CurrencyTypes.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ImmutableDictionary<int, CurrencyTypeSnapshot>.Builder byId =
                ImmutableDictionary.CreateBuilder<int, CurrencyTypeSnapshot>();
            ImmutableDictionary<CurrencyKind, int>.Builder idByKind =
                ImmutableDictionary.CreateBuilder<CurrencyKind, int>();

            foreach (CurrencyTypeEntity entity in entities)
            {
                CurrencyTypeSnapshot snapshot = new CurrencyTypeSnapshot
                {
                    Id = entity.Id,
                    Name = entity.Name ?? string.Empty,
                    CurrencyType = entity.CurrencyType,
                    ActivityPointType = entity.ActivityPointType,
                    Enabled = entity.Enabled,
                    StartingAmount = entity.StartingAmount,
                };

                CurrencyKind kind = new CurrencyKind
                {
                    CurrencyType = snapshot.CurrencyType,
                    ActivityPointType = snapshot.ActivityPointType,
                };

                idByKind[kind] = snapshot.Id;
                byId[snapshot.Id] = snapshot;
            }

            State state = new State(byId.ToImmutable(), idByKind.ToImmutable());

            _state = state;

            _logger.LogInformation("Loaded currency type mapping: Count={Count}", state.ById.Count);
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
