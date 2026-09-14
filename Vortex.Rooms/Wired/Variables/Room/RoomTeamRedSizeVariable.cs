using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>How many players are on the team. (red)</summary>
public sealed class RoomTeamRedSizeVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.red.size";
    protected override string TeamKey => "red";
    protected override ushort Order => 41;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamMemberCount(team);
}
