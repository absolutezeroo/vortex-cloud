using Vortex.Primitives.Messages.Incoming.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Avatar;

internal class ChangeUserNameMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new ChangeUserNameMessage();
}
