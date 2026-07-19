using Vortex.Primitives.Messages.Incoming.Advertisement;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Advertisement;

internal class InterstitialShownMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new InterstitialShownMessage();
}
