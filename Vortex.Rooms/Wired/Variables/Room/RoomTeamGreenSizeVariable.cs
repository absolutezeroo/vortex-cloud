using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>How many players are on the team. (green)</summary>
public sealed class RoomTeamGreenSizeVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.green.size";
    protected override string TeamKey => "green";
    protected override ushort Order => 43;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamMemberCount(team);
}
