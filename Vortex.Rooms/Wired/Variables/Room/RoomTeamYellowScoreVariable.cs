using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>The points the team has taken. (yellow)</summary>
public sealed class RoomTeamYellowScoreVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.yellow.score";
    protected override string TeamKey => "yellow";
    protected override ushort Order => 46;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamScore(team);
}
