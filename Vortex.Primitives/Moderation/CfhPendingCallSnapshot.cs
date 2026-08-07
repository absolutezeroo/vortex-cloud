using Orleans;

namespace Vortex.Primitives.Moderation;

/// <summary>
/// One of a player's own open reports, as their client lists it back to them.
/// </summary>
/// <remarks>
/// All three fields are strings on the wire, the id included — the client reads them with
/// <c>readString</c> and only ever concatenates the messages for display, so nothing is gained by
/// pretending the id is a number.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record CfhPendingCallSnapshot
{
    [Id(0)]
    public required string CallId { get; init; }

    [Id(1)]
    public required string TimeStamp { get; init; }

    [Id(2)]
    public required string Message { get; init; }
}
