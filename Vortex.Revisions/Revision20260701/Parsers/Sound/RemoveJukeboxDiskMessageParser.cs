using Vortex.Primitives.Messages.Incoming.Sound;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Sound;

internal class RemoveJukeboxDiskMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RemoveJukeboxDiskMessage();
}
