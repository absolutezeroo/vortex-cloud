using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Orleans.Snapshots.Vault;
using Vortex.Primitives.Vault.Enums;

namespace Vortex.Primitives.Players.Grains;

public interface IPlayerVaultGrain : IGrainWithIntegerKey
{
    Task<List<IncomeRewardSnapshot>> GetIncomeRewardsAsync(CancellationToken ct);

    Task<bool> ClaimCategoryAsync(VaultRewardCategoryType category, CancellationToken ct);

    Task AddIncomeRewardAsync(
        VaultRewardCategoryType category,
        VaultRewardType type,
        int amount,
        string productCode,
        CancellationToken ct
    );
}
