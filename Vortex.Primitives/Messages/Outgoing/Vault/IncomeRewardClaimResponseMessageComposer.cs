using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Vault.Enums;

namespace Vortex.Primitives.Messages.Outgoing.Vault;

[GenerateSerializer, Immutable]
public sealed record IncomeRewardClaimResponseMessageComposer : IComposer
{
    [Id(0)]
    public required VaultRewardCategoryType RewardCategory { get; init; }

    [Id(1)]
    public required bool Result { get; init; }
}
