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

/// <summary>One key/value pair in a config's or a status's <c>extra</c> map.</summary>
[GenerateSerializer, Immutable]
public readonly record struct WiredVariableFxExtra(
    [property: Id(0)] string Key,
    [property: Id(1)] string Value
);

/// <summary>
/// The composite key a Variable FX status is addressed by.
/// </summary>
/// <remarks>
/// Written in one place because the client parses it by hand — <c>indexOf('|')</c> for the config
/// id, the last <c>|</c> for the entity id, the one before it for the user/furni letter — so a key
/// built any other way is read as a different display, silently, and the bar stops updating rather
/// than erroring.
/// </remarks>
public static class WiredVariableFxKey
{
    public static string Build(int configId, string variableId, bool isUserEntity, int entityId) =>
        $"{configId}|{variableId}|{(isUserEntity ? "u" : "f")}|{entityId}";
}
