namespace Turbo.Primitives.Observability;

/// <summary>
/// Default immutable <see cref="ITurboContext"/> carrier. Identity fields are optional so the
/// context can be created at packet entry, before the player/room have been resolved, and a
/// refined copy can be produced later via <see cref="WithIdentity"/>.
/// </summary>
public sealed class TurboContext(
    CorrelationId correlationId,
    string operation,
    string? sessionKey = null,
    long? playerId = null,
    int? roomId = null
) : ITurboContext
{
    public CorrelationId CorrelationId { get; } = correlationId;
    public string Operation { get; } = operation;
    public string? SessionKey { get; } = sessionKey;
    public long? PlayerId { get; } = playerId;
    public int? RoomId { get; } = roomId;

    public TurboContext WithIdentity(long? playerId, int? roomId) =>
        new(CorrelationId, Operation, SessionKey, playerId ?? PlayerId, roomId ?? RoomId);
}
