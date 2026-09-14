using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>The points the team has taken. (blue)</summary>
public sealed class RoomTeamBlueScoreVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.blue.score";
    protected override string TeamKey => "blue";
    protected override ushort Order => 44;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamScore(team);
}
