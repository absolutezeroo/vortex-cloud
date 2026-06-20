using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Catalog;
using Turbo.Primitives.Players.Providers;
using Turbo.Primitives.Players.Snapshots;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.Players.Providers;

public sealed class CurrencyTypeProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<ICurrencyTypeProvider> logger
) : ICurrencyTypeProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<ICurrencyTypeProvider> _logger = logger;
    private readonly Dictionary<int, CurrencyTypeSnapshot> _currenciesById = [];
    private readonly Dictionary<CurrencyKind, int> _currencyIdsByKind = [];

    public CurrencyTypeSnapshot? GetCurrencyType(int typeId)
    {
        if (!_currenciesById.TryGetValue(typeId, out CurrencyTypeSnapshot? snapshot))
        {
            return null;
        }

        return snapshot;
    }

    public CurrencyTypeSnapshot? GetCurrencyTypeByKind(CurrencyKind kind)
    {
        if (!_currencyIdsByKind.TryGetValue(kind, out int id))
        {
            return null;
        }

        return GetCurrencyType(id);
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        _currenciesById.Clear();
        _currencyIdsByKind.Clear();

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<CurrencyTypeEntity> entities = await dbCtx
                .CurrencyTypes.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (CurrencyTypeEntity entity in entities)
            {
                CurrencyTypeSnapshot snapshot = new CurrencyTypeSnapshot
                {
                    Id = entity.Id,
                    Name = entity.Name ?? string.Empty,
                    CurrencyType = entity.CurrencyType,
                    ActivityPointType = entity.ActivityPointType,
                    Enabled = entity.Enabled,
                };

                CurrencyKind kind = new CurrencyKind
                {
                    CurrencyType = snapshot.CurrencyType,
                    ActivityPointType = snapshot.ActivityPointType,
                };

                _currencyIdsByKind[kind] = snapshot.Id;
                _currenciesById[snapshot.Id] = snapshot;
            }

            _logger.LogInformation(
                "Loaded currency type mapping: Count={Count}",
                _currenciesById.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
