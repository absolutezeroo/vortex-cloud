using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Which team the avatar is on, 0 when none — the room's own team book, for the same reason
/// <see cref="Variables.Room.RoomTeamVariable"/> reads it: an arena whose team space IS the Habbo
/// four shares that book, and one whose is not deliberately keeps its teams to itself.
/// <para>
/// The official client presents this as a subtree rather than one number, and what its leaves are
/// called is not recoverable from anything on this side — so this is the flat reading, under the
/// team ordinals <see cref="TeamSet.HabboColours"/> already numbers the colours with, which are the
/// same ordinals the coloured gates and scoreboards use.
/// </para>
/// </summary>
public sealed class UserTeamVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@team";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 180;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.AlwaysAvailable
        | WiredVariableFlags.HasTextConnector;

    protected override Dictionary<WiredVariableValue, string> GetTextConnectors()
    {
        Dictionary<WiredVariableValue, string> connectors = new()
        {
            [WiredVariableValue.Parse(0)] = "none",
        };

        foreach (GameTeam team in TeamSet.HabboColours.Teams)
        {
            connectors[WiredVariableValue.Parse(team.Id.Value)] = team.Key;
        }

        return connectors;
    }

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(
            _roomGrain.GameRuntime.RoomTeams.GetTeam(avatar.PlayerId).Value
        );

        return true;
    }
}
