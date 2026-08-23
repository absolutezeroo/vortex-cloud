using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Vault.Enums;
using Vortex.Protocol.Messages.Incoming.Vault;

namespace Vortex.Revisions.Revision20260701.Parsers.Vault;

internal class IncomeRewardClaimMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new IncomeRewardClaimMessage { Category = (VaultRewardCategoryType)packet.PopByte() };
}
