using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Vault;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Wallet;
using Turbo.Primitives.Vault.Enums;

namespace Turbo.Players.Grains;

internal sealed class PlayerVaultGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory
) : Grain, IPlayerVaultGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;

    private readonly List<PlayerVaultIncomeRewardEntity> _pendingRewards = [];

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);
    }

    public Task<List<IncomeRewardSnapshot>> GetIncomeRewardsAsync(CancellationToken ct)
    {
        var snapshots = _pendingRewards
            .Select(r => new IncomeRewardSnapshot
            {
                RewardCategory = (VaultRewardCategoryType)r.RewardCategory,
                RewardType = (VaultRewardType)r.RewardType,
                Amount = r.Amount,
                ProductCode = r.ProductCode,
            })
            .ToList();

        return Task.FromResult(snapshots);
    }

    public async Task<bool> ClaimCategoryAsync(
        VaultRewardCategoryType category,
        CancellationToken ct
    )
    {
        var toGrant = _pendingRewards
            .Where(r => (VaultRewardCategoryType)r.RewardCategory == category)
            .ToList();

        if (toGrant.Count == 0)
            return false;

        var walletGrain = _grainFactory.GetPlayerWalletGrain(this.GetPrimaryKeyLong());

        foreach (var reward in toGrant)
        {
            switch ((VaultRewardType)reward.RewardType)
            {
                case VaultRewardType.Credits:
                    await walletGrain.GrantCreditsAsync(reward.Amount, ct).ConfigureAwait(false);
                    break;

                case VaultRewardType.Duckets:
                    await walletGrain
                        .GrantActivityPointsAsync(0, reward.Amount, ct)
                        .ConfigureAwait(false);
                    break;
            }
        }

        var ids = toGrant.Select(r => r.Id).ToList();

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        await dbCtx
            .PlayerVaultIncomeRewards.Where(r => ids.Contains(r.Id))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        _pendingRewards.RemoveAll(r => (VaultRewardCategoryType)r.RewardCategory == category);

        return true;
    }

    public async Task AddIncomeRewardAsync(
        VaultRewardCategoryType category,
        VaultRewardType type,
        int amount,
        string productCode,
        CancellationToken ct
    )
    {
        if (amount <= 0 && string.IsNullOrEmpty(productCode))
            return;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(productCode))
        {
            var existing = _pendingRewards.FirstOrDefault(r =>
                (VaultRewardCategoryType)r.RewardCategory == category
                && (VaultRewardType)r.RewardType == type
                && string.IsNullOrEmpty(r.ProductCode)
            );

            if (existing is not null)
            {
                existing.Amount += amount;
                await dbCtx
                    .PlayerVaultIncomeRewards.Where(r => r.Id == existing.Id)
                    .ExecuteUpdateAsync(up => up.SetProperty(p => p.Amount, existing.Amount), ct)
                    .ConfigureAwait(false);
                return;
            }
        }

        var entity = new PlayerVaultIncomeRewardEntity
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            RewardCategory = (int)category,
            RewardType = (int)type,
            Amount = amount,
            ProductCode = productCode ?? string.Empty,
        };

        dbCtx.PlayerVaultIncomeRewards.Add(entity);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        _pendingRewards.Add(entity);
    }

    private async Task HydrateAsync(CancellationToken ct)
    {
        _pendingRewards.Clear();

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var rows = await dbCtx
            .PlayerVaultIncomeRewards.AsNoTracking()
            .Where(r => r.PlayerEntityId == (int)this.GetPrimaryKeyLong())
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _pendingRewards.AddRange(rows);
    }
}
