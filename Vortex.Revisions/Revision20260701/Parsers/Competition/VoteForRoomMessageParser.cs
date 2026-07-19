using Vortex.Primitives.Messages.Incoming.Competition;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Competition;

internal class VoteForRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new VoteForRoomMessage();
}
