using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Benchmark;

/// <summary>
/// One fake player, on a real socket.
/// </summary>
/// <remarks>
/// <para>
/// It speaks the wire and nothing else: it does not decode a single composer. Every frame the server
/// sends is measured and thrown away, except the latency answer, which is the one packet whose
/// content matters here. That is what keeps this honest and small — a client that understood the
/// protocol would need five hundred parsers kept in step with the real ones, and would still only be
/// used to count bytes.
/// </para>
/// <para>
/// Framing, both directions: <c>[int32 length][int16 header][body]</c>, big-endian, length covering
/// the header. Strings are <c>[uint16 length][UTF-8]</c>.
/// </para>
/// <para>
/// <b>No encryption.</b> The client skips the Diffie handshake, and the server's decoder treats a
/// session with no key as plaintext — so RC4 is the one layer of the real client's path this does
/// not exercise. Every other layer is the real one: accept, framing, parsers, handlers, grains and
/// the per-session fan-out on the way back.
/// </para>
/// </remarks>
internal sealed class SyntheticClient(string host, int port) : IDisposable
{
    private const int SsoTicketHeader = 882;
    private const int OpenFlatConnectionHeader = 3234;
    private const int MoveAvatarHeader = 2364;
    private const int ChatHeader = 3034;
    private const int LatencyPingRequestHeader = 544;
    private const int LatencyPingResponseHeader = 188;

    private readonly TcpClient _tcp = new();
    private readonly ConcurrentDictionary<int, long> _pending = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private NetworkStream? _stream;
    private int _nextRequestId;

    public long PacketsReceived;
    public long BytesReceived;
    public long Failures;

    /// <summary>Completed round trips, in ticks, drained by the collector once a second.</summary>
    public ConcurrentQueue<long> RoundTrips { get; } = new();

    public bool Connected => _tcp.Connected;

    public async Task ConnectAsync(string ticket, CancellationToken ct)
    {
        await _tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);

        _tcp.NoDelay = true;
        _stream = _tcp.GetStream();

        // Nagle off and straight into the handshake. The client's own opening exchange (hello,
        // version, unique id) is skipped: none of it gates the ticket, and including it would
        // measure three handlers that a session performs exactly once.
        await SendAsync(SsoTicketHeader, w => w.String(ticket).Int(0), ct).ConfigureAwait(false);
    }

    public Task EnterRoomAsync(int roomId, CancellationToken ct) =>
        SendAsync(OpenFlatConnectionHeader, w => w.Int(roomId).String(string.Empty).Int(-1), ct);

    public Task WalkAsync(int x, int y, CancellationToken ct) =>
        SendAsync(MoveAvatarHeader, w => w.Int(x).Int(y), ct);

    public Task SayAsync(string text, CancellationToken ct) =>
        SendAsync(ChatHeader, w => w.String(text).Int(0).Int(-1), ct);

    /// <summary>
    /// Sends the probe whose answer is timed. The id is what comes back, so a slow answer is still
    /// matched to the right send rather than to whichever ping happened to be outstanding.
    /// </summary>
    public Task PingAsync(CancellationToken ct)
    {
        int requestId = Interlocked.Increment(ref _nextRequestId);

        _pending[requestId] = Stopwatch.GetTimestamp();

        return SendAsync(LatencyPingRequestHeader, w => w.Int(requestId), ct);
    }

    /// <summary>
    /// Reads until the socket closes. Runs for the whole life of the client: an unread socket fills
    /// its receive buffer, the server's writes then block, and the run would end up measuring its
    /// own back-pressure.
    /// </summary>
    public async Task ReceiveLoopAsync(CancellationToken ct)
    {
        if (_stream is null)
        {
            return;
        }

        byte[] header = new byte[4];

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!await ReadExactlyAsync(header, 4, ct).ConfigureAwait(false))
                {
                    return;
                }

                int length = BinaryPrimitives.ReadInt32BigEndian(header);

                if (length is < 2 or > (1 << 20))
                {
                    Interlocked.Increment(ref Failures);

                    return;
                }

                byte[] body = new byte[length];

                if (!await ReadExactlyAsync(body, length, ct).ConfigureAwait(false))
                {
                    return;
                }

                Interlocked.Increment(ref PacketsReceived);
                Interlocked.Add(ref BytesReceived, length + 4);

                if (BinaryPrimitives.ReadInt16BigEndian(body) == LatencyPingResponseHeader)
                {
                    CompletePing(body);
                }
            }
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // The socket went away mid-run. Counted, not thrown: one client dropping is a result,
            // and taking the whole run down with it would throw away the other 399.
            Interlocked.Increment(ref Failures);
        }
    }

    private void CompletePing(byte[] body)
    {
        if (body.Length < 6)
        {
            return;
        }

        int requestId = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(2, 4));

        if (_pending.TryRemove(requestId, out long sentAt))
        {
            RoundTrips.Enqueue(Stopwatch.GetTimestamp() - sentAt);
        }
    }

    private async Task<bool> ReadExactlyAsync(byte[] buffer, int count, CancellationToken ct)
    {
        int read = 0;

        while (read < count)
        {
            int got = await _stream!
                .ReadAsync(buffer.AsMemory(read, count - read), ct)
                .ConfigureAwait(false);

            if (got == 0)
            {
                return false;
            }

            read += got;
        }

        return true;
    }

    private async Task SendAsync(
        int header,
        Action<BenchmarkPacketWriter> build,
        CancellationToken ct
    )
    {
        if (_stream is null)
        {
            return;
        }

        BenchmarkPacketWriter writer = new(header);

        build(writer);

        byte[] payload = writer.ToArray();

        // One send at a time: the walk timer, the chat timer and the ping timer all write to this
        // socket, and two interleaved frames would desynchronise the server's reader for good.
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            await _stream.WriteAsync(payload, ct).ConfigureAwait(false);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            Interlocked.Increment(ref Failures);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        _writeLock.Dispose();
        _stream?.Dispose();
        _tcp.Dispose();
    }
}
