using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Gifts;

namespace Vortex.Revisions.Revision20260701.Parsers.Gifts;

internal class ResetPhoneNumberStateMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new ResetPhoneNumberStateMessage();
}
