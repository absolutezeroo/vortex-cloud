using System;

namespace Vortex.Networking.Configuration;

public class NetworkingConfig
{
    public const string SECTION_NAME = "Vortex:Networking";

    /// <summary>
    ///     Default cap on a single client-declared packet body length, in bytes. Bounds
    ///     memory/CPU exposure from a malformed or hostile frame header.
    /// </summary>
    public const int DefaultMaxPacketBodyBytes = 65536;

    public int PingIntervalMilliseconds { get; init; } = 10000;

    /// <summary>
    ///     How long a session may stay silent before the heartbeat sends it a PING. Anything the
    ///     client sends counts as activity, so a session in normal use is never pinged.
    /// </summary>
    public TimeSpan IdleOkActivityWindow { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     How long a session may stay silent before it is closed as dead.
    ///     <see cref="TimeSpan.Zero" /> — the default — disables closing entirely.
    ///
    ///     Disabled on purpose, and this is not timidity: for a browser client, silence is not
    ///     evidence of death. Chrome FREEZES a backgrounded tab after roughly five minutes, which
    ///     stops all JavaScript — the WebSocket 'message' handler included — so the client cannot
    ///     answer a PING no matter how long it is given. It is not gone; it resumes the moment the
    ///     user comes back, and the socket is still open underneath. Closing on silence therefore
    ///     guarantees that every backgrounded tab is disconnected, which is the exact bug this
    ///     heartbeat was added to fix. Measured: pings answered for four minutes with the tab
    ///     hidden, then a hard stop, then a close 150 s later.
    ///
    ///     A genuinely dead peer is reported by the transport (the session's Closed handler), which
    ///     needs no timer. Set a non-zero value only if you have a reason the above does not cover.
    /// </summary>
    public TimeSpan PongTimeout { get; init; } = TimeSpan.Zero;

    public int MaxPacketBodyBytes { get; init; } = DefaultMaxPacketBodyBytes;
}
