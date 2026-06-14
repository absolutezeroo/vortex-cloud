using Turbo.Primitives.Networking;
using Turbo.Primitives.Vault.Enums;

namespace Turbo.Primitives.Messages.Incoming.Vault;

public record IncomeRewardClaimMessage : IMessageEvent
{
    public required VaultRewardCategoryType Category { get; init; }
}
