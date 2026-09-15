using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

/// <summary>One value of a shared wired variable, as the rooms that read it see it.</summary>
/// <remarks>
/// The storage key is the same one the in-memory stores use, so a value written by the owning room
/// and one read by a referencing room are addressed identically on both sides.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredSharedValueSnapshot
{
    [Id(0)]
    public required string StorageKey { get; init; }

    [Id(1)]
    public required int Value { get; init; }

    /// <summary>Unix milliseconds, for the "variable age" condition.</summary>
    [Id(2)]
    public required long CreatedAtMs { get; init; }

    [Id(3)]
    public required long UpdatedAtMs { get; init; }
}
