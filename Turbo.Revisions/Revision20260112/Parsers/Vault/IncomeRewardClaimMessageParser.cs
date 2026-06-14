using Turbo.Primitives.Messages.Incoming.Vault;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Vault.Enums;

namespace Turbo.Revisions.Revision20260112.Parsers.Vault;

internal class IncomeRewardClaimMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new IncomeRewardClaimMessage
        {
            Category = (VaultRewardCategoryType)packet.PopByte(),
        };
}
