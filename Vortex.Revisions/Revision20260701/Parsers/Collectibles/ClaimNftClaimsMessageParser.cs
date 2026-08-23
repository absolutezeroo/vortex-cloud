using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Collectibles;

namespace Vortex.Revisions.Revision20260701.Parsers.Collectibles;

internal class ClaimNftClaimsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ClaimNftClaimsMessage { ClaimId = packet.PopString(), Wallet = packet.PopString() };
}
