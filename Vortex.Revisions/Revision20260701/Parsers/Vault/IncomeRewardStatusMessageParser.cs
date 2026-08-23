using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Vault;

namespace Vortex.Revisions.Revision20260701.Parsers.Vault;

internal class IncomeRewardStatusMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new IncomeRewardStatusMessage();
}
