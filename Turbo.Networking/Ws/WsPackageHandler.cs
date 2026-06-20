using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.WebSocket;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Networking.Ws;

internal sealed class WsPackageHandler(
    IClientPacketDecoder decoder,
    IPackageHandler<IClientPacket> inner
) : IPackageHandler<WebSocketPackage>
{
    private readonly IClientPacketDecoder _decoder = decoder;
    private readonly IPackageHandler<IClientPacket> _inner = inner;

    public async ValueTask Handle(
        IAppSession session,
        WebSocketPackage package,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(package);

        if (session is not ISessionContext ctx || package.OpCode != OpCode.Binary)
        {
            return;
        }

        foreach (ReadOnlyMemory<byte> segment in package.Data)
        {
            ctx.WsBuffer?.Write(segment.Span);
        }

        while (true)
        {
            if (ctx.WsBuffer is null)
            {
                break;
            }

            ReadOnlyMemory<byte> memory = ctx.WsBuffer.WrittenMemory;

            if (memory.Length == 0)
            {
                break;
            }

            SequenceReader<byte> reader = new(
                new ReadOnlySequence<byte>(memory)
            );

            IClientPacket? packet = _decoder.TryRead(ref reader, ctx);

            if (packet is null)
            {
                break;
            }

            ctx.WsBuffer.Clear();
            ctx.WsBuffer.Write(ctx.WsBuffer.WrittenSpan[(int)reader.Consumed..]);

            await _inner.Handle(session, packet, ct).ConfigureAwait(false);
        }
    }
}
