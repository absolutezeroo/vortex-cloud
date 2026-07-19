using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Packets;

public interface IParser
{
    public IMessageEvent Parse(IClientPacket packet);
}
