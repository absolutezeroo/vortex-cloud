using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.Room;

/// <summary>
/// How many players are in the room.
/// </summary>
/// <remarks>
/// Players, not avatars: <c>AvatarsByPlayerId</c> rather than <c>AvatarsByObjectId</c>, because the
/// second also holds pets and bots and a builder reading "@user_count" means the people.
/// </remarks>
public sealed class RoomUserCountVariable(RoomGrain roomGrain) : RoomVariable(roomGrain)
{
    protected override string VariableName => "@user_count";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 20;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForRoom(RoomGrain roomGrain) =>
        WiredVariableValue.Parse(roomGrain._state.AvatarsByPlayerId.Count);
}
