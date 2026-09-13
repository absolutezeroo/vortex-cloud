using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// One Variable FX display the room has declared: what it looks like and what it is bound to.
/// </summary>
/// <remarks>
/// Field names and order are the client's own — <c>VariableFxConfigUpdateData</c>'s constructor
/// assigns them in exactly this sequence, which is also the order they go on the wire.
/// <para>
/// Three of them are still nameless in the client: it carries them as obfuscated identifiers and
/// nothing in the decompiled tree says what they mean. They are named for their position here rather
/// than guessed at, because a wrong name in a contract outlives the guess that produced it.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredVariableFxConfigSnapshot
{
    [Id(0)]
    public required int ConfigId { get; init; }

    /// <summary>Whether the display hangs off users rather than furni.</summary>
    [Id(1)]
    public required bool IsUserFx { get; init; }

    [Id(2)]
    public required int ShowMode { get; init; }

    /// <summary>Wire field 4. The client calls it <c>_SafeStr_5289</c>; the add-on's own form sends
    /// the same unnamed quantity at int param 3.</summary>
    [Id(3)]
    public int Field4 { get; init; }

    [Id(4)]
    public required bool ShowOnMouseHover { get; init; }

    [Id(5)]
    public required int ShowDuration { get; init; }

    /// <summary>Which of the six boxes this is: health points 0, and one per box after it.</summary>
    [Id(6)]
    public required int CategoryId { get; init; }

    /// <summary>Wire field 8, <c>_SafeStr_5449</c>. Appears nowhere else in the client.</summary>
    [Id(7)]
    public int Field8 { get; init; }

    [Id(8)]
    public required int ColorId { get; init; }

    /// <summary>Wire field 10, <c>_SafeStr_5624</c>; the add-on's form sends it at int param 8.</summary>
    [Id(9)]
    public int Field10 { get; init; }

    [Id(10)]
    public required int RendererId { get; init; }

    [Id(11)]
    public required long DefaultMinValue { get; init; }

    [Id(12)]
    public required long DefaultMaxValue { get; init; }

    /// <summary>Free-form string pairs the client keeps beside the config and hands back to the
    /// renderer.</summary>
    [Id(13)]
    public ImmutableArray<WiredVariableFxExtra> Extra { get; init; } = [];
}
