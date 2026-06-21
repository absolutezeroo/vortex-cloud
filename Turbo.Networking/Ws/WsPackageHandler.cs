using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.WebSocket;
using Turbo.Networking.Package;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Networking.Ws;

internal sealed class WsPackageHandler(IClientPacketDecoder decoder, PackageHandler inner)
    : IPackageHandler<WebSocketPackage>
{
    private readonly IClientPacketDecoder _decoder = decoder;
    private readonly PackageHandler _inner = inner;

    /// <summary>
    ///     Standard handler for DI-based sessions.
    /// </summary>
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

        await ProcessPackageAsync(ctx, package, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Handler for WebSocket transports that keep the Turbo session context separate
    ///     from the SuperSocket session object.
    /// </summary>
    public async ValueTask HandleManualAsync(
        ISessionContext ctx,
        WebSocketPackage package,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.OpCode != OpCode.Binary)
        {
            return;
        }

        await ProcessPackageAsync(ctx, package, ct).ConfigureAwait(false);
    }

    private async ValueTask ProcessPackageAsync(
        ISessionContext ctx,
        WebSocketPackage package,
        CancellationToken ct
    )
    {
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

            SequenceReader<byte> reader = new(new ReadOnlySequence<byte>(memory));

            IClientPacket? packet = _decoder.TryRead(ref reader, ctx);

            if (packet is null)
            {
                break;
            }

            byte[] remaining = ctx.WsBuffer.WrittenSpan[(int)reader.Consumed..].ToArray();

            ctx.WsBuffer.Clear();

            if (remaining.Length > 0)
            {
                ctx.WsBuffer.Write(remaining);
            }

            await _inner.HandleAsync(ctx, packet, ct).ConfigureAwait(false);
        }
    }
}
