using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Competition;

namespace Vortex.Revisions.Revision20260701.Parsers.Competition;

internal class RoomCompetitionInitMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RoomCompetitionInitMessage();
}
