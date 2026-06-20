using System;
using Microsoft.Extensions.Logging;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.ErrorTracking;

/// <summary>
/// Non-blocking sink for pipeline runtime errors. It enriches a technical error occurrence with
/// ambient operation metadata and enqueues it for background persistence.
/// </summary>
internal sealed class ChannelErrorGroupingSink(
    ErrorGroupingChannel channel,
    ITurboContextAccessor contextAccessor,
    ILogger<ChannelErrorGroupingSink> logger
) : IErrorGroupingSink
{
    private readonly ErrorGroupingChannel _channel = channel;
    private readonly ITurboContextAccessor _contextAccessor = contextAccessor;
    private readonly ILogger<ChannelErrorGroupingSink> _logger = logger;

    public void Record(
        Exception exception,
        string source,
        string operation,
        long? actorId = null,
        int? roomId = null,
        string? correlationId = null,
        string? sessionKey = null,
        string? remoteIp = null
    )
    {
        ITurboContext? current = _contextAccessor.Current;
        long? resolvedActorId = actorId ?? current?.PlayerId;
        int? resolvedRoomId = roomId ?? current?.RoomId;
        string? resolvedCorrelationId = correlationId ?? current?.CorrelationId.Value;
        string? resolvedSessionKey = sessionKey ?? current?.SessionKey;
        DateTime occurredAt = DateTime.UtcNow;
        ErrorGroupingRecord record = ErrorGroupingRecord.FromException(
            exception,
            source,
            operation,
            occurredAt,
            resolvedActorId,
            resolvedRoomId,
            resolvedCorrelationId,
            resolvedSessionKey,
            remoteIp
        );

        if (!_channel.TryWrite(record))
        {
            _logger.LogWarning(
                TurboEventIds.ErrorGroupingDropped,
                "Runtime error grouping event dropped (channel saturated): {Source}/{Operation}",
                source,
                operation
            );
        }
    }
}
