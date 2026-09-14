using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// The shared half of <c>@teams.&lt;colour&gt;.score</c> and <c>.size</c>.
/// </summary>
/// <remarks>
/// The official client offers these flat, per colour, with no mention of which game they belong to —
/// and that turns out to be this codebase's own shape rather than a simplification of it. An arena
/// whose team space IS the Habbo four shares the room's one book
/// (<c>RoomGameRuntime.RoomTeams</c>), which is every shipped game; only an arena with a team space
/// the four colours cannot express gets a private book, and such an arena deliberately does not
/// raise the wired score event either, because two sources of truth for one <c>bb_score_r</c> would
/// only make it flicker.
/// <para>
/// So there is no arena to choose between: the room's book is the Habbo-facing answer by design, and
/// naming these per arena instead would invent the second truth the runtime goes out of its way to
/// avoid. The teams of an exotic arena are invisible here, and that is the same silence the
/// scoreboards already keep.
/// </para>
/// </remarks>
public abstract class RoomTeamVariable(RoomGrain roomGrain) : RoomVariable(roomGrain)
{
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;

    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    /// <summary>The colour, by the key <see cref="TeamSet.HabboColours"/> declares it under —
    /// resolved rather than hard-coded, so the ordinals stay the set's business.</summary>
    protected abstract string TeamKey { get; }

    protected override WiredVariableValue GetValueForRoom(RoomGrain roomGrain)
    {
        GameTeam? team = TeamSet.HabboColours.FindByKey(TeamKey);

        return team is null
            ? WiredVariableValue.Default
            : WiredVariableValue.Parse(Read(roomGrain.GameRuntime.RoomTeams, team.Id));
    }

    protected abstract int Read(TeamBook teams, TeamId team);
}
