using System.Buffers;
using SuperSocket.ProtoBase;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Networking.Tcp;

internal sealed class TcpFilter(IClientPacketDecoder decoder) : PipelineFilterBase<IClientPacket>
{
    private readonly IClientPacketDecoder _decoder = decoder;

    public override IClientPacket Filter(ref SequenceReader<byte> reader)
    {
        if (Context is not ISessionContext ctx)
        {
            return null!;
        }

        SequenceReader<byte> r = reader;
        IClientPacket? packet = _decoder.TryRead(ref r, ctx);

        if (packet is null)
        {
            return null!;
        }

        reader = r;

        return packet;
    }
}
