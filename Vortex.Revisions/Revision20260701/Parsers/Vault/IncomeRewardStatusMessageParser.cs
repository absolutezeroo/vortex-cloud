using Vortex.Primitives.Messages.Incoming.Vault;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Vault;

internal class IncomeRewardStatusMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new IncomeRewardStatusMessage();
}
