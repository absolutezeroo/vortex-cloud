using Vortex.Primitives.Messages.Incoming.Camera;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Camera;

internal class PurchasePhotoMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PurchasePhotoMessage();
}
