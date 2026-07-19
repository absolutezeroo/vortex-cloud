using System.Buffers;
using Vortex.Primitives.Packets;

namespace Vortex.Primitives.Networking;

public interface IClientPacketDecoder
{
    public IClientPacket TryRead(ref SequenceReader<byte> reader, ISessionContext ctx);
}
