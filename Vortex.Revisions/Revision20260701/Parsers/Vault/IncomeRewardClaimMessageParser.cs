using Vortex.Primitives.Messages.Incoming.Vault;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Vault.Enums;

namespace Vortex.Revisions.Revision20260701.Parsers.Vault;

internal class IncomeRewardClaimMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new IncomeRewardClaimMessage { Category = (VaultRewardCategoryType)packet.PopByte() };
}
