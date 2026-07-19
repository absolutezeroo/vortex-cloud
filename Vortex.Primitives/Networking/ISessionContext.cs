using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Crypto;

namespace Vortex.Primitives.Networking;

public interface ISessionContext
{
    public SessionKey SessionKey { get; }
    public string RevisionId { get; }
    public DateTime LastActivityUtc { get; }
    public CancellationTokenSource HeartbeatCts { get; }
    public string? RemoteIpAddress { get; }
    public IRc4Engine? CryptoIn { get; }
    public IRc4Engine? CryptoOut { get; }
    public ArrayBufferWriter<byte>? WsBuffer { get; }
    public Task CloseSessionAsync();
    public void Touch();
    public void SetRevisionId(string revisionId);
    public void SetupEncryption(byte[] key, bool setCryptoOut = false);
    public Task SendComposerAsync(IComposer composer, CancellationToken ct);
}
