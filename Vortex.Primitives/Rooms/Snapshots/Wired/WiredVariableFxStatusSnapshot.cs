using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// What one Variable FX display currently reads, for one entity.
/// </summary>
/// <remarks>
/// The client re-derives <c>configId</c>, <c>variableId</c>, <c>isUserEntity</c> and
/// <c>entityId</c> by splitting <see cref="StatusKey"/> on <c>|</c>, and separately reads the last
/// two off the wire. Both halves have to agree: see <see cref="WiredVariableFxKey"/>, which is the
/// only place that format is written.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredVariableFxStatusSnapshot
{
    /// <summary>
    /// <c>configId|variableId|u-or-f|entityId</c>.
    /// </summary>
    [Id(0)]
    public required string StatusKey { get; init; }

    /// <summary>First value this display has been given, as opposed to a change to one it already
    /// shows. The message also carries a flag that forces this on for every entry it holds.</summary>
    [Id(1)]
    public required bool IsInitialize { get; init; }

    [Id(2)]
    public required bool IsUserEntity { get; init; }

    /// <summary>The avatar or the furni the display hangs off, per <see cref="IsUserEntity"/>.</summary>
    [Id(3)]
    public required int EntityId { get; init; }

    [Id(4)]
    public required long Value { get; init; }

    /// <summary>Bounds for this entity alone, overriding the config's defaults. Both or neither:
    /// the client reads them behind a single flag.</summary>
    [Id(5)]
    public long? OverrideMinValue { get; init; }

    [Id(6)]
    public long? OverrideMaxValue { get; init; }

    [Id(7)]
    public ImmutableArray<WiredVariableFxExtra> Extra { get; init; } = [];
}
