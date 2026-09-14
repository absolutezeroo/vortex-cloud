using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>The points the team has taken. (green)</summary>
public sealed class RoomTeamGreenScoreVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.green.score";
    protected override string TeamKey => "green";
    protected override ushort Order => 42;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamScore(team);
}
