namespace Vortex.Primitives.Observability;

/// <summary>
/// Default immutable <see cref="IVortexContext"/> carrier. Identity fields are optional so the
/// context can be created at packet entry, before the player/room have been resolved, and a
/// refined copy can be produced later via <see cref="WithIdentity"/>.
/// </summary>
public sealed class VortexContext(
    CorrelationId correlationId,
    string operation,
    string? sessionKey = null,
    long? playerId = null,
    int? roomId = null
) : IVortexContext
{
    public CorrelationId CorrelationId { get; } = correlationId;
    public string Operation { get; } = operation;
    public string? SessionKey { get; } = sessionKey;
    public long? PlayerId { get; } = playerId;
    public int? RoomId { get; } = roomId;

    public VortexContext WithIdentity(long? playerId, int? roomId) =>
        new(CorrelationId, Operation, SessionKey, playerId ?? PlayerId, roomId ?? RoomId);
}
