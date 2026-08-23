using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Advertisement;

namespace Vortex.Revisions.Revision20260701.Parsers.Advertisement;

internal class GetInterstitialMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetInterstitialMessage();
}
