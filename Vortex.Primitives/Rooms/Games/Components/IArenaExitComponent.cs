namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// The tile that takes a player out of a match — walked onto voluntarily (a forfeit) or arrived at
/// after elimination. A game with no exit component simply cannot relocate an eliminated player,
/// which is why the arena validator reports it as a warning rather than pretending it is optional.
/// </summary>
public interface IArenaExitComponent : IGameComponent;
