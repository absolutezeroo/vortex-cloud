using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;

namespace Turbo.Observability.ErrorTracking;

/// <summary>
/// Bounded channel for non-blocking technical error grouping writes.
/// Records are never written to the database on hot path.
/// </summary>
internal sealed class ErrorGroupingChannel
{
    private readonly Channel<ErrorGroupingRecord> _channel;

    public ErrorGroupingChannel(IOptions<ObservabilityConfig> options)
    {
        int capacity =
            options.Value.ErrorGroupingChannelCapacity > 0
                ? options.Value.ErrorGroupingChannelCapacity
                : 10_000;

        _channel = Channel.CreateBounded<ErrorGroupingRecord>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }
        );
    }

    internal ChannelReader<ErrorGroupingRecord> Reader => _channel.Reader;

    internal bool TryWrite(ErrorGroupingRecord record) => _channel.Writer.TryWrite(record);
}
