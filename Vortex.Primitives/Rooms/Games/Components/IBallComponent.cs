namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// A kickable ball. The component is only the furni's identity and visual state — where the ball is,
/// where it is going and how fast lives in the game's own simulation, never on the furni, so that a
/// ball cannot outlive the match that set it moving.
/// </summary>
public interface IBallComponent : IGameComponent;
