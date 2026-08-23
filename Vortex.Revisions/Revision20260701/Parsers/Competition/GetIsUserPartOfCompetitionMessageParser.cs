using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Competition;

namespace Vortex.Revisions.Revision20260701.Parsers.Competition;

internal class GetIsUserPartOfCompetitionMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetIsUserPartOfCompetitionMessage { GoalCode = packet.PopString() };
}
