using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Vault;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.Vault.Enums;

namespace Vortex.Players.Grains;

internal sealed class PlayerVaultGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory
) : Grain, IPlayerVaultGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;

    private readonly List<PlayerVaultIncomeRewardEntity> _pendingRewards = [];

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);
    }

    public Task<List<IncomeRewardSnapshot>> GetIncomeRewardsAsync(CancellationToken ct)
    {
        List<IncomeRewardSnapshot> snapshots = _pendingRewards
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
        List<PlayerVaultIncomeRewardEntity> toGrant = _pendingRewards
            .Where(r => (VaultRewardCategoryType)r.RewardCategory == category)
            .ToList();

        if (toGrant.Count == 0)
        {
            return false;
        }

        IPlayerWalletGrain walletGrain = _grainFactory.GetPlayerWalletGrain(
            this.GetPrimaryKeyLong()
        );

        foreach (PlayerVaultIncomeRewardEntity reward in toGrant)
        {
            switch ((VaultRewardType)reward.RewardType)
            {
                case VaultRewardType.Credits:
                    await walletGrain.GrantCreditsAsync(reward.Amount, ct).ConfigureAwait(true);
                    break;

                case VaultRewardType.Duckets:
                    await walletGrain
                        .GrantActivityPointsAsync(0, reward.Amount, ct)
                        .ConfigureAwait(true);
                    break;
            }
        }

        List<int> ids = toGrant.Select(r => r.Id).ToList();

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        await dbCtx
            .PlayerVaultIncomeRewards.Where(r => ids.Contains(r.Id))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

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
        {
            return;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        if (string.IsNullOrEmpty(productCode))
        {
            PlayerVaultIncomeRewardEntity? existing = _pendingRewards.FirstOrDefault(r =>
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
                    .ConfigureAwait(true);
                return;
            }
        }

        PlayerVaultIncomeRewardEntity entity = new PlayerVaultIncomeRewardEntity
        {
            PlayerEntityId = (int)this.GetPrimaryKeyLong(),
            RewardCategory = (int)category,
            RewardType = (int)type,
            Amount = amount,
            ProductCode = productCode ?? string.Empty,
        };

        dbCtx.PlayerVaultIncomeRewards.Add(entity);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        _pendingRewards.Add(entity);
    }

    private async Task HydrateAsync(CancellationToken ct)
    {
        _pendingRewards.Clear();

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        List<PlayerVaultIncomeRewardEntity> rows = await dbCtx
            .PlayerVaultIncomeRewards.AsNoTracking()
            .Where(r => r.PlayerEntityId == (int)this.GetPrimaryKeyLong())
            .ToListAsync(ct)
            .ConfigureAwait(true);

        _pendingRewards.AddRange(rows);
    }
}
