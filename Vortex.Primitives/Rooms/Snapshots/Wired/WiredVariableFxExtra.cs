using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>One key/value pair in a Variable FX config's or status's <c>extra</c> map.</summary>
[GenerateSerializer, Immutable]
public readonly record struct WiredVariableFxExtra(
    [property: Id(0)] string Key,
    [property: Id(1)] string Value
);
