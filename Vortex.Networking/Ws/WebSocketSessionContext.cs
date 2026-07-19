using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperSocket.ProtoBase;
using SuperSocket.Server.Abstractions;
using SuperSocket.WebSocket.Server;
using Vortex.Crypto;
using Vortex.Primitives.Crypto;
using Vortex.Primitives.Networking;

namespace Vortex.Networking.Ws;

/// <summary>
///     WebSocket-backed session context used by the shared packet/session pipeline.
/// </summary>
public sealed class WebSocketSessionContext(
    WebSocketSession session,
    IPackageEncoder<OutgoingPackage> packageEncoder,
    ILogger<WebSocketSessionContext> logger
) : ISessionContext, IDisposable
{
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    public SessionKey SessionKey { get; } = session.SessionID;
    public string RevisionId { get; private set; } = "Default";
    public DateTime LastActivityUtc { get; private set; } = DateTime.UtcNow;
    public CancellationTokenSource HeartbeatCts { get; } = new();

    public string? RemoteIpAddress { get; } =
        session.RemoteEndPoint is IPEndPoint ipEndpoint ? ipEndpoint.Address?.ToString() : null;

    public IRc4Engine? CryptoIn { get; private set; }
    public IRc4Engine? CryptoOut { get; private set; }
    public ArrayBufferWriter<byte>? WsBuffer { get; } = new(4096);

    public async Task CloseSessionAsync()
    {
        await session.CloseAsync().ConfigureAwait(false);
    }

    public void Touch()
    {
        LastActivityUtc = DateTime.UtcNow;
    }

    public void SetRevisionId(string revisionId)
    {
        RevisionId = revisionId;
    }

    public void SetupEncryption(byte[] key, bool setCryptoOut = false)
    {
        CryptoIn = new Rc4Engine(key);

        if (setCryptoOut)
        {
            CryptoOut = new Rc4Engine(key);
        }
    }

    public async Task SendComposerAsync(IComposer composer, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }

        await _sendSemaphore.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (session.State == SessionState.Closed)
            {
                return;
            }

            ArrayBufferWriter<byte> writer = new(4096);
            int bytesWritten = packageEncoder.Encode(writer, new OutgoingPackage(this, composer));

            if (bytesWritten <= 0)
            {
                return;
            }

            byte[] payload = writer.WrittenSpan.ToArray();

            await session.SendAsync(payload).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            logger.LogDebug(
                ex,
                "Cancelled sending composer {ComposerType} to websocket session {SessionKey}",
                composer?.GetType().Name ?? "<null>",
                SessionKey
            );
        }
        catch (Exception ex)
        {
            logger.LogDebug(
                ex,
                "Failed to send composer {ComposerType} to websocket session {SessionKey}",
                composer?.GetType().Name ?? "<null>",
                SessionKey
            );
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    public void Dispose()
    {
        HeartbeatCts.Dispose();
        _sendSemaphore.Dispose();
    }
}
