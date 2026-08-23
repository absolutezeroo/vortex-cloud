using Vortex.Primitives.Networking;
using Vortex.Primitives.Vault.Enums;

namespace Vortex.Protocol.Messages.Incoming.Vault;

public record IncomeRewardClaimMessage : IMessageEvent
{
    public required VaultRewardCategoryType Category { get; init; }
}
