using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Camera;

namespace Vortex.Revisions.Revision20260701.Parsers.Camera;

internal class RequestCameraConfigurationMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new RequestCameraConfigurationMessage();
}
