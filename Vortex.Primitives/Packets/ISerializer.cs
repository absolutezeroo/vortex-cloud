using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Packets;

public interface ISerializer
{
    public int Header { get; }

    public IServerPacket Serialize(IComposer message);
}
