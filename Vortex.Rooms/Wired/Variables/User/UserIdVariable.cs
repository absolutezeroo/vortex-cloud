using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// The account id behind the avatar — what a chain stores when it wants to recognise someone on a
/// later visit, where <c>@index</c> is only good for as long as they stand in the room.
/// </summary>
public sealed class UserIdVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@user_id";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 170;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse((int)avatar.PlayerId);

        return true;
    }
}
