using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>How many players are on the team. (yellow)</summary>
public sealed class RoomTeamYellowSizeVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.yellow.size";
    protected override string TeamKey => "yellow";
    protected override ushort Order => 47;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamMemberCount(team);
}
