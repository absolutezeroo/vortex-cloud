namespace Vortex.Primitives.Observability;

/// <summary>
/// Ambient, read-only view of the operation currently being processed. Resolved through
/// <see cref="IVortexContextAccessor"/>; it is never passed explicitly through method signatures.
/// </summary>
public interface IVortexContext
{
    CorrelationId CorrelationId { get; }

    /// <summary>Human-readable label for the operation (usually the incoming message type name).</summary>
    string Operation { get; }

    string? SessionKey { get; }

    long? PlayerId { get; }

    int? RoomId { get; }
}
