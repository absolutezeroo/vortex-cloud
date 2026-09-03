using System;

namespace Vortex.Primitives.Rooms.Games;

/// <summary>
/// A room game's stable identity — "banzai", "freeze", "football". It is a string rather than an
/// enum on purpose: a game contributed by a plugin assembly cannot add a member to a core enum, and
/// the framework's whole point is that adding a game touches no core file.
/// <para>
/// The value is the same token that labels the game's slice of the room tick in the metrics and that
/// appears in every structured log line, so keep it short, lowercase and permanent.
/// </para>
/// </summary>
public readonly record struct GameId(string Value)
{
    public static readonly GameId None = new(string.Empty);

    public bool IsNone => string.IsNullOrEmpty(Value);

    public override string ToString() => Value;
}
