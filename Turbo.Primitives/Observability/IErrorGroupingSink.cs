using System;

namespace Turbo.Primitives.Observability;

/// <summary>
/// Non-blocking sink for runtime error grouping. Implementations should enqueue and return quickly;
/// they must never run durable DB writes on the caller path.
/// </summary>
public interface IErrorGroupingSink
{
    void Record(
        Exception exception,
        string source,
        string operation,
        long? actorId = null,
        int? roomId = null,
        string? correlationId = null,
        string? sessionKey = null,
        string? remoteIp = null
    );
}
