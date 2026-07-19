using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.WebSocket;
using Vortex.Networking.Package;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Networking.Ws;

internal sealed class WsPackageHandler(
    IClientPacketDecoder decoder,
    PackageHandler inner,
    ILogger<WsPackageHandler>? logger = null
) : IPackageHandler<WebSocketPackage>
{
    private readonly IClientPacketDecoder _decoder = decoder;
    private readonly PackageHandler _inner = inner;
    private readonly ILogger<WsPackageHandler>? _logger = logger;

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

            IClientPacket? packet;

            try
            {
                packet = _decoder.TryRead(ref reader, ctx);
            }
            catch (InvalidDataException ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Closing websocket session {SessionKey} ({RemoteIpAddress}) after an invalid packet frame",
                    ctx.SessionKey,
                    ctx.RemoteIpAddress
                );

                ctx.WsBuffer.Clear();

                await ctx.CloseSessionAsync().ConfigureAwait(false);

                return;
            }

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
