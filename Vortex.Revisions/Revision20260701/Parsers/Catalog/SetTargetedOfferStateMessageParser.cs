using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Catalog;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class SetTargetedOfferStateMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetTargetedOfferStateMessage
        {
            TargetedOfferId = packet.PopInt(),
            TrackingState = packet.PopInt(),
        };
}
