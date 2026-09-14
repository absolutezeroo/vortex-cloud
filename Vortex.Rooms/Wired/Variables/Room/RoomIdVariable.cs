using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// The room's own id — what a builder compares against to tell one of their rooms from another.
/// </summary>
public sealed class RoomIdVariable(RoomGrain roomGrain) : RoomVariable(roomGrain)
{
    protected override string VariableName => "@room_id";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Meta;
    protected override ushort Order => 50;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForRoom(RoomGrain roomGrain) =>
        WiredVariableValue.Parse(roomGrain._state.RoomId.Value);
}
