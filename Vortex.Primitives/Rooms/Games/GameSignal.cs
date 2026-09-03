using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Games.Components;

namespace Vortex.Primitives.Rooms.Games;

/// <summary>What happened to a game component.</summary>
public enum GameSignalKind
{
    /// <summary>An avatar stepped onto the component's tile.</summary>
    WalkOn = 0,

    /// <summary>An avatar stepped off the component's tile.</summary>
    WalkOff = 1,

    /// <summary>A player used (double-clicked) the component. <c>Param</c> carries the client's
    /// button/state argument.</summary>
    Use = 2,

    /// <summary>The furni left the room — picked up, moved, or the room unloading. The one signal a
    /// game gets for "part of my arena just disappeared".</summary>
    Detached = 3,
}

/// <summary>
/// One thing that happened to one game component, on its way from the furniture to the game that
/// owns it. This is the whole vocabulary the room speaks to games: the room does not know what
/// Battle Banzai is, it knows a player walked onto a component, and the runtime knows which game
/// that component belongs to.
/// <para>
/// A struct on purpose — a busy Banzai arena raises one of these per tile step per player, and the
/// hot path should not allocate to say so.
/// </para>
/// </summary>
public readonly record struct GameSignal(
    GameSignalKind Kind,
    IGameComponent Component,
    PlayerId Player,
    int Param
)
{
    public static GameSignal WalkOn(IGameComponent component, PlayerId player) =>
        new(GameSignalKind.WalkOn, component, player, 0);

    public static GameSignal WalkOff(IGameComponent component, PlayerId player) =>
        new(GameSignalKind.WalkOff, component, player, 0);

    public static GameSignal Use(IGameComponent component, PlayerId player, int param) =>
        new(GameSignalKind.Use, component, player, param);

    public static GameSignal Detached(IGameComponent component) =>
        new(GameSignalKind.Detached, component, default, 0);
}
