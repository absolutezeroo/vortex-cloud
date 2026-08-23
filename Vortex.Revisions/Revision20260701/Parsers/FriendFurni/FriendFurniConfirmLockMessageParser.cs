using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Friendfurni;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendFurni;

internal class FriendFurniConfirmLockMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new FriendFurniConfirmLockMessage();
}
