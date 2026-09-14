using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>The points the team has taken. (red)</summary>
public sealed class RoomTeamRedScoreVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.red.score";
    protected override string TeamKey => "red";
    protected override ushort Order => 40;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamScore(team);
}
