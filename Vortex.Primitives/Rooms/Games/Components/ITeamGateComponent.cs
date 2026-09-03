using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// The tile a player stands on to join (or leave) a team before a match. Its furni state shows the
/// team's current member count, which the runtime repaints — no game does it by hand.
/// </summary>
public interface ITeamGateComponent : IGameComponent
{
    GameTeamColor Team { get; }
}
