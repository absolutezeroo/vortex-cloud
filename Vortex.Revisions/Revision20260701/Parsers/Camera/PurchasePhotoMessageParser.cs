using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Camera;

namespace Vortex.Revisions.Revision20260701.Parsers.Camera;

internal class PurchasePhotoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PurchasePhotoMessage();
}
