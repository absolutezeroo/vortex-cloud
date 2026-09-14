using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// The group the room belongs to, or 0 when it belongs to none.
/// </summary>
/// <remarks>
/// Zero rather than absent: every other reading in this band is always available, and a builder
/// testing "@group_id != 0" is the natural way to ask "is this room a guild room". Making the
/// variable unavailable instead would take the whole chain down with it.
/// </remarks>
public sealed class RoomGroupIdVariable(RoomGrain roomGrain) : RoomVariable(roomGrain)
{
    protected override string VariableName => "@group_id";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Meta;
    protected override ushort Order => 60;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForRoom(RoomGrain roomGrain) =>
        WiredVariableValue.Parse(roomGrain._state.RoomSnapshot?.GroupId ?? 0);
}
