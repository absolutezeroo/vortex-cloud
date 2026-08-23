using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class GetCfhStatusMessageParser : IParser
{
    // Nothing to read: WIN63's composer for 3458 returns an empty message array. This used to pop a
    // boolean off a packet with no body.
    public IMessageEvent Parse(IClientPacket packet) => new GetCfhStatusMessage();
}
