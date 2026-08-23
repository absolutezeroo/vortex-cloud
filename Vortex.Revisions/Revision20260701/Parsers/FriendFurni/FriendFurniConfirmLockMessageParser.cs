using Vortex.Protocol.Messages.Incoming.Friendfurni;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendFurni;

internal class FriendFurniConfirmLockMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new FriendFurniConfirmLockMessage();
}
