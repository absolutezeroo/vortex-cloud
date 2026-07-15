using System.Threading.Channels;

namespace Turbo.Rooms.Wired.Logs;

/// <summary>
/// Bounded, in-process queue decoupling wired-execution log emission (hot path, one writer per
/// room tick) from persistence (single background writer). Bounded so a database stall can never
/// grow memory without limit; a full channel silently drops the entry rather than blocking wired
/// execution — mirrors <c>Turbo.Observability.Audit.AuditChannel</c>.
/// </summary>
public sealed class RoomWiredLogChannel
{
    private readonly Channel<RoomWiredLogEntry> _channel = Channel.CreateBounded<RoomWiredLogEntry>(
        new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    internal ChannelReader<RoomWiredLogEntry> Reader => _channel.Reader;

    /// <summary>Non-blocking enqueue. Returns false when the channel is saturated.</summary>
    internal bool TryWrite(RoomWiredLogEntry entry) => _channel.Writer.TryWrite(entry);
}
