using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// A team scoreboard. Deliberately NOT an <see cref="IGameComponent"/>: the <c>furniture_score</c>
/// boards are room chrome shared by every game (and by a wired give-score box outside any match), so
/// they belong to no single game and are painted by the runtime's presentation layer off the score
/// events rather than by whichever game happens to be running.
/// </summary>
public interface IScoreDisplayComponent : IFurnitureLogic
{
    GameTeamColor Team { get; }
}
