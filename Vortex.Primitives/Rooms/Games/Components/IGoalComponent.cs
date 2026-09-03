using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// A goal mouth. A ball entering it scores for <see cref="Team"/> — the colour the furni carries,
/// consistent with every other coloured game furni in the catalogue (a <c>bb_gate_r</c> is red's
/// gate, a <c>bb_score_r</c> is red's board) and with the reference emulator, which credits the
/// scoreboards of the goal's own colour.
/// <para>
/// Habbo's own rule here is <b>not authoritatively known</b>: no capture of the official server
/// exists and the client has no football logic to read it from. Crediting the goal's colour is what
/// the reference implementation does — evidence, not authority — and the goal event carries the
/// kicker so that an own goal stays distinguishable downstream instead of being flattened into the
/// team total.
/// </para>
/// </summary>
public interface IGoalComponent : IGameComponent
{
    GameTeamColor Team { get; }

    /// <summary>
    /// The way the net faces, which is the way its mouth opens. A ball only goes in when it is
    /// travelling roughly INTO that mouth — rolling past the back or the side of a net leaves it
    /// standing — so the direction gate lives on the goal rather than in the ball's rules.
    /// </summary>
    Rotation Facing { get; }
}
