using Vortex.Primitives.Messages.Incoming.Talent;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Talent;

internal class GuideAdvertisementReadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GuideAdvertisementReadMessage();
}
