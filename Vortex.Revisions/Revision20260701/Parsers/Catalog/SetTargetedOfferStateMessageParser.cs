using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

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
