using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>How many players are on the team. (blue)</summary>
public sealed class RoomTeamBlueSizeVariable(RoomGrain roomGrain) : RoomTeamVariable(roomGrain)
{
    protected override string VariableName => "@teams.blue.size";
    protected override string TeamKey => "blue";
    protected override ushort Order => 45;

    protected override int Read(TeamBook teams, TeamId team) => teams.GetTeamMemberCount(team);
}
