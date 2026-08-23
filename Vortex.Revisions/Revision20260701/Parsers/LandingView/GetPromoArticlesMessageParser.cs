using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Landingview;

namespace Vortex.Revisions.Revision20260701.Parsers.LandingView;

internal class GetPromoArticlesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetPromoArticlesMessage();
}
