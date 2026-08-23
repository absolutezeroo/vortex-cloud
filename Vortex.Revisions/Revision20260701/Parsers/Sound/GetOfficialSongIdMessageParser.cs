using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Sound;

namespace Vortex.Revisions.Revision20260701.Parsers.Sound;

internal class GetOfficialSongIdMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetOfficialSongIdMessage();
}
