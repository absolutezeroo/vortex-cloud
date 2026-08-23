using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Talent;

namespace Vortex.Revisions.Revision20260701.Parsers.Talent;

internal class GetTalentTrackLevelMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetTalentTrackLevelMessage();
}
