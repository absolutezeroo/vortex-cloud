using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Quest;

internal class RejectQuestMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RejectQuestMessage { QuestId = packet.PopInt() };
}
