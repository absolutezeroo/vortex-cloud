using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// A goal mouth. A ball entering it scores for <see cref="Team"/> — the colour the furni carries,
/// consistent with every other coloured game furni in the catalogue (a <c>bb_gate_r</c> is red's
/// gate, a <c>bb_score_r</c> is red's board), and the only reading that generalises past two teams.
/// <para>
/// Habbo's own rule here is <b>unknown</b>: no capture says whether a goal credits its own colour or
/// the opponent. The choice above is an assumption, and the goal event carries the kicker so that an
/// own goal stays distinguishable downstream instead of being flattened into the team total.
/// </para>
/// </summary>
public interface IGoalComponent : IGameComponent
{
    GameTeamColor Team { get; }
}
