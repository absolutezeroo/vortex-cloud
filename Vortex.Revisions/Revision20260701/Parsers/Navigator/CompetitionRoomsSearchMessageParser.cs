using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class CompetitionRoomsSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CompetitionRoomsSearchMessage { GoalId = packet.PopInt(), PageIndex = packet.PopInt() };
}
