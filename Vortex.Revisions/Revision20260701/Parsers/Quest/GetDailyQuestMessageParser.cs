using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Quest;

namespace Vortex.Revisions.Revision20260701.Parsers.Quest;

internal class GetDailyQuestMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetDailyQuestMessage();
}
