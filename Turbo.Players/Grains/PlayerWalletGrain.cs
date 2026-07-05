using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Events;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Providers;
using Turbo.Primitives.Players.Snapshots;
using Turbo.Primitives.Players.Wallet;

namespace Turbo.Players.Grains;

internal sealed class PlayerWalletGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ICurrencyTypeProvider currencyTypeProvider,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<PlayerWalletGrain> logger
) : Grain, IPlayerWalletGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ICurrencyTypeProvider _currencyTypeProvider = currencyTypeProvider;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<PlayerWalletGrain> _logger = logger;

    private (string Currency, int? ActivityPointType) DescribeCurrency(CurrencyKind kind)
    {
        // Resolve the human currency name from the currency_types table; fall back to the enum name.
        string name =
            _currencyTypeProvider.GetCurrencyTypeByKind(kind)?.Name ?? kind.CurrencyType.ToString();

        int? activityPointType =
            kind.CurrencyType == CurrencyType.ActivityPoints ? kind.ActivityPointType : null;

        return (name, activityPointType);
    }

    private readonly Dictionary<CurrencyKind, WalletCurrencySnapshot> _currenciesByKind = [];

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);
    }

    public async Task<WalletDebitResult> TryDebitAsync(
        List<WalletDebitRequest> requests,
        CancellationToken ct
    )
    {
        if (
            TryNormalizeRequests(requests, out List<WalletDebitRequest> normalizedRequests)
            && normalizedRequests.Count > 0
        )
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);
            await using IDbContextTransaction tx = await dbCtx
                .Database.BeginTransactionAsync(ct)
                .ConfigureAwait(true);

            List<WalletCurrencyUpdateSnapshot> updates = new List<WalletCurrencyUpdateSnapshot>(
                normalizedRequests.Count
            );

            foreach (WalletDebitRequest request in normalizedRequests)
            {
                try
                {
                    WalletCurrencyUpdateSnapshot update = await ProcessDebitRequestAsync(
                        dbCtx,
                        request,
                        ct
                    );

                    if (update.ChangedBy != request.Amount)
                    {
                        throw new Exception("Failed to process debit request");
                    }

                    updates.Add(update);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Wallet debit failed for player {PlayerId} ({Currency} x{Amount})",
                        this.GetPrimaryKeyLong(),
                        request.CurrencyKind.CurrencyType,
                        request.Amount
                    );

                    await tx.RollbackAsync(ct);
                    await RollbackUpdatesAsync(updates, ct);

                    return WalletDebitResult.InsufficientBalance(
                        new WalletDebitFailure
                        {
                            CurrencyKind = request.CurrencyKind,
                            Amount = request.Amount,
                        }
                    );
                }
            }

            await dbCtx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(
                (int)this.GetPrimaryKeyLong()
            );

            foreach (WalletCurrencyUpdateSnapshot update in updates)
            {
                await playerPresence.OnCurrencyUpdateAsync(update, ct);

                (string currency, int? activityPointType) = DescribeCurrency(update.CurrencyKind);

                await _events
                    .PublishAsync(
                        new CurrencyChangedEvent(
                            (int)this.GetPrimaryKeyLong(),
                            currency,
                            activityPointType,
                            -update.ChangedBy,
                            update.Amount
                        ),
                        ct
                    )
                    .ConfigureAwait(true);
            }
        }

        return WalletDebitResult.Success();
    }

    public Task RollbackUpdatesAsync(
        List<WalletCurrencyUpdateSnapshot> updates,
        CancellationToken ct
    )
    {
        if (updates.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (WalletCurrencyUpdateSnapshot? update in updates)
        {
            if (update is null || update.ChangedBy == 0)
            {
                continue;
            }

            if (
                _currenciesByKind.TryGetValue(
                    update.CurrencyKind,
                    out WalletCurrencySnapshot? snapshot
                )
            )
            {
                _currenciesByKind[update.CurrencyKind] = snapshot with
                {
                    Amount = snapshot.Amount + update.ChangedBy,
                };
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> GetAmountForCurrencyAsync(CurrencyKind kind, CancellationToken ct) =>
        Task.FromResult(
            _currenciesByKind.TryGetValue(kind, out WalletCurrencySnapshot? snapshot)
                ? snapshot.Amount
                : 0
        );

    public Task<Dictionary<int, int>> GetActivityPointsAsync(CancellationToken ct)
    {
        Dictionary<int, int> result = new Dictionary<int, int>();

        foreach (WalletCurrencySnapshot? currency in _currenciesByKind.Values)
        {
            if (
                currency is null
                || currency.CurrencyKind.CurrencyType != CurrencyType.ActivityPoints
            )
            {
                continue;
            }

            result[currency.CurrencyKind.ActivityPointType ?? -1] = currency.Amount;
        }

        return Task.FromResult(result);
    }

    private static bool TryNormalizeRequests(
        List<WalletDebitRequest> proposed,
        out List<WalletDebitRequest> normalized
    )
    {
        normalized = [];

        Dictionary<CurrencyKind, int> totals = new Dictionary<CurrencyKind, int>(proposed.Count);

        foreach (WalletDebitRequest? request in proposed)
        {
            if (request is null || request.Amount <= 0)
            {
                continue;
            }

            int cost = request.Amount;

            if (totals.TryGetValue(request.CurrencyKind, out int total))
            {
                cost += total;
            }

            totals[request.CurrencyKind] = cost;
        }

        foreach ((CurrencyKind kind, int total) in totals)
        {
            if (total <= 0)
            {
                continue;
            }

            normalized.Add(new WalletDebitRequest { CurrencyKind = kind, Amount = total });
        }

        return true;
    }

    private async Task<WalletCurrencyUpdateSnapshot> ProcessDebitRequestAsync(
        TurboDbContext dbCtx,
        WalletDebitRequest request,
        CancellationToken ct
    )
    {
        int changedBy = 0;
        int currentAmount = 0;
        int cost = request.Amount;

        if (
            _currenciesByKind.TryGetValue(
                request.CurrencyKind,
                out WalletCurrencySnapshot? snapshot
            )
        )
        {
            PlayerCurrencyEntity? entity = await dbCtx
                .PlayerCurrencies.Where(x =>
                    x.Id == snapshot.Id && x.PlayerEntityId == (int)this.GetPrimaryKeyLong()
                )
                .FirstOrDefaultAsync(ct);

            if (entity is not null)
            {
                currentAmount = entity.Amount;

                if ((cost > 0) && (currentAmount >= cost))
                {
                    changedBy = cost;
                    entity.Amount -= changedBy;
                    currentAmount = entity.Amount;
                }
            }

            _currenciesByKind[request.CurrencyKind] = snapshot with { Amount = currentAmount };
        }

        return new()
        {
            CurrencyKind = request.CurrencyKind,
            ChangedBy = changedBy,
            Amount = currentAmount,
        };
    }

    public Task GrantCreditsAsync(int amount, CancellationToken ct) =>
        GrantCurrencyAsync(new CurrencyKind { CurrencyType = CurrencyType.Credits }, amount, ct);

    public Task GrantActivityPointsAsync(int activityPointType, int amount, CancellationToken ct) =>
        GrantCurrencyAsync(
            new CurrencyKind
            {
                CurrencyType = CurrencyType.ActivityPoints,
                ActivityPointType = activityPointType,
            },
            amount,
            ct
        );

    public async Task CreditBackAsync(List<WalletDebitRequest> requests, CancellationToken ct)
    {
        foreach (WalletDebitRequest request in requests)
        {
            await GrantCurrencyAsync(request.CurrencyKind, request.Amount, ct).ConfigureAwait(true);
        }
    }

    private async Task GrantCurrencyAsync(CurrencyKind kind, int amount, CancellationToken ct)
    {
        if (amount <= 0)
        {
            return;
        }

        if (!_currenciesByKind.TryGetValue(kind, out WalletCurrencySnapshot? snapshot))
        {
            return;
        }

        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerCurrencyEntity? entity = await dbCtx
            .PlayerCurrencies.Where(x =>
                x.Id == snapshot.Id && x.PlayerEntityId == (int)this.GetPrimaryKeyLong()
            )
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            return;
        }

        entity.Amount += amount;
        await dbCtx.SaveChangesAsync(ct);

        _currenciesByKind[kind] = snapshot with { Amount = entity.Amount };

        (string currencyName, int? activityPointType) = DescribeCurrency(kind);

        await _events
            .PublishAsync(
                new CurrencyChangedEvent(
                    (int)this.GetPrimaryKeyLong(),
                    currencyName,
                    activityPointType,
                    amount,
                    entity.Amount
                ),
                ct
            )
            .ConfigureAwait(true);

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(
            (int)this.GetPrimaryKeyLong()
        );
        await playerPresence.OnCurrencyUpdateAsync(
            new WalletCurrencyUpdateSnapshot
            {
                CurrencyKind = kind,
                ChangedBy = amount,
                Amount = entity.Amount,
            },
            ct
        );
    }

    private async Task HydrateAsync(CancellationToken ct)
    {
        _currenciesByKind.Clear();

        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        List<PlayerCurrencyEntity> entities = await dbCtx
            .PlayerCurrencies.AsNoTracking()
            .Where(x => x.PlayerEntityId == (int)this.GetPrimaryKeyLong())
            .ToListAsync(ct);

        foreach (PlayerCurrencyEntity entity in entities)
        {
            CurrencyTypeSnapshot? currencyType = _currencyTypeProvider.GetCurrencyType(
                entity.CurrencyTypeEntityId
            );

            if (currencyType is null || !currencyType.Enabled)
            {
                continue;
            }

            WalletCurrencySnapshot snapshot = new WalletCurrencySnapshot
            {
                Id = entity.Id,
                CurrencyKind = new CurrencyKind
                {
                    CurrencyType = currencyType.CurrencyType,
                    ActivityPointType = currencyType.ActivityPointType,
                },
                Amount = entity.Amount,
            };

            _currenciesByKind[snapshot.CurrencyKind] = snapshot;
        }

        CurrencyKind creditsKind = new CurrencyKind { CurrencyType = CurrencyType.Credits };

        if (!_currenciesByKind.ContainsKey(creditsKind))
        {
            CurrencyTypeSnapshot? creditType = _currencyTypeProvider.GetCurrencyTypeByKind(
                creditsKind
            );

            if (creditType is not null)
            {
                await using TurboDbContext writeCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

                PlayerCurrencyEntity entity = new PlayerCurrencyEntity
                {
                    PlayerEntityId = (int)this.GetPrimaryKeyLong(),
                    CurrencyTypeEntityId = creditType.Id,
                    Amount = 200,
                };

                writeCtx.PlayerCurrencies.Add(entity);

                await writeCtx.SaveChangesAsync(ct).ConfigureAwait(true);

                _currenciesByKind[creditsKind] = new WalletCurrencySnapshot
                {
                    Id = entity.Id,
                    CurrencyKind = creditsKind,
                    Amount = entity.Amount,
                };
            }
        }
    }
}
