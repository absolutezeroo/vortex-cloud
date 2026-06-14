using System.Threading;
using System.Threading.Tasks;
using System;
using Turbo.Messages.Registry;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Messages.Incoming.Tracking;

namespace Turbo.PacketHandlers.Tracking;

public class PerformanceLogMessageHandler(IPerformanceLogSink performanceLogSink)
    : IMessageHandler<PerformanceLogMessage>
{
    private readonly IPerformanceLogSink _performanceLogSink = performanceLogSink;

    public async ValueTask HandleAsync(
        PerformanceLogMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        var eventEntry = new PerformanceLogEvent
        {
            ElapsedTime = message.ElapsedTime,
            UserAgent = message.UserAgent ?? "unknown",
            FlashVersion = message.FlashVersion ?? "unknown",
            OS = message.OS ?? "unknown",
            Browser = message.Browser ?? "unknown",
            IsDebugger = message.IsDebugger,
            MemoryUsage = message.MemoryUsage,
            GarbageCollections = message.GarbageCollections,
            AverageFrameRate = message.AverageFrameRate,
            IPAddress = string.IsNullOrWhiteSpace(ctx.RemoteIpAddress)
                ? "unknown"
                : ctx.RemoteIpAddress,
        };

        _performanceLogSink.Record(eventEntry);

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
