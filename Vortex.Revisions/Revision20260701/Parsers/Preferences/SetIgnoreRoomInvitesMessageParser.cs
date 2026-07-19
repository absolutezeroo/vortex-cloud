using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

internal class SetIgnoreRoomInvitesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new SetIgnoreRoomInvitesMessage();
}
