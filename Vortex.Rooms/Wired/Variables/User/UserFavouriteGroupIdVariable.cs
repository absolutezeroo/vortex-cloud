using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// The guild whose badge the avatar is wearing. This is the client's own "no badge" sentinel of -1
/// rather than 0, because that is the number the avatar carries and the number every other consumer
/// of it compares against; normalising it here would put a second spelling of "no guild" into the
/// codebase.
/// </summary>
public sealed class UserFavouriteGroupIdVariable(RoomGrain roomGrain)
    : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@favourite_group_id";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 140;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(avatar.GroupId);

        return true;
    }
}
