using System.Collections.Immutable;

namespace Vortex.Primitives.Signals;

/// <summary>
/// A named fact a signal can carry, and everything an editor needs to let an operator filter on it.
/// </summary>
/// <remarks>
/// <para>
/// The kind is what makes a filter editor generic. Before it, the dashboard hardcoded which facts
/// got a picker and which got a text box, and a floor/wall filter was a select written by hand on
/// one page — so any other system wanting filters would have had to write all of it again.
/// </para>
/// <para>
/// The key is stored in content (<c>reward_track_step_filters.fact_key</c> and the equivalent
/// columns of any later consumer). It is therefore append-only: a renamed key silently stops
/// matching every filter already written on it, which no build and no test would notice. Add keys,
/// never rename or remove them.
/// </para>
/// </remarks>
/// <param name="LabelKey">Dashboard locale key.</param>
/// <param name="FallbackLabel">
/// Shown when the locale key resolves to nothing. A plugin cannot add keys to the dashboard's
/// locales, so without this its facts would render as raw keys; the core fills it too, which keeps
/// the editor readable when a translation lags behind.
/// </param>
/// <param name="EnumValues">
/// The allowed values, for <see cref="FactKind.Enum"/> only. A closed fact with no values declared
/// is a select with no options, and is refused when the vocabulary is built.
/// </param>
public sealed record FactKey(
    string Key,
    FactKind Kind,
    string LabelKey,
    string FallbackLabel,
    ImmutableArray<EnumValue> EnumValues = default
);

/// <summary>One allowed value of a closed fact: what the engine compares, and what the operator reads.</summary>
public readonly record struct EnumValue(string Value, string LabelKey, string FallbackLabel);

/// <summary>
/// What a fact's value is, which decides the control an editor shows and the operators it offers.
/// </summary>
public enum FactKind
{
    /// <summary>Free text — a room name, a description, a motto. Only <c>Contains</c> is much use.</summary>
    Text = 0,

    /// <summary>A count, a level, a total. May legitimately be zero.</summary>
    Number = 1,

    /// <summary>A player id. Offers the player picker.</summary>
    PlayerId = 2,

    /// <summary>A room id. Offers the room picker.</summary>
    RoomId = 3,

    /// <summary>A furniture definition id. Offers the furniture picker, with its sprite.</summary>
    FurnitureId = 4,

    /// <summary>A navigator flat-category id. Offers the category list.</summary>
    CategoryId = 5,

    /// <summary>A badge code.</summary>
    BadgeCode = 6,

    /// <summary>A catalogue offer id.</summary>
    OfferId = 7,

    /// <summary>A closed set of values, enumerated in <see cref="FactKey.EnumValues"/>.</summary>
    Enum = 8,

    /// <summary>
    /// A live id with no directory behind it — a placed item, a pet. Typed by hand, and mostly
    /// useful as the target of a <c>$N</c> back-reference rather than as a literal.
    /// </summary>
    OpaqueId = 9,
}
