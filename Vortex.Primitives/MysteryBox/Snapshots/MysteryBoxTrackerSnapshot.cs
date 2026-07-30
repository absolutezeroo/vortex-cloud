using Orleans;

namespace Vortex.Primitives.MysteryBox.Snapshots;

/// <summary>
/// What the player currently holds, as shown by the client's mystery box toolbar tracker. Either
/// side may be empty, which hides that half of the tracker. A box and a key only pair up when the
/// two colours match.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record MysteryBoxTrackerSnapshot
{
    [Id(0)]
    public required string BoxColor { get; init; }

    [Id(1)]
    public required string KeyColor { get; init; }

    public static MysteryBoxTrackerSnapshot Empty { get; } =
        new() { BoxColor = string.Empty, KeyColor = string.Empty };
}
