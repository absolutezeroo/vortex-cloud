using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

public abstract class UserVariable<TAvatar>(RoomGrain roomGrain) : WiredInternalVariable(roomGrain)
    where TAvatar : IRoomAvatar
{
    protected override WiredVariableTargetType TargetType => WiredVariableTargetType.User;

    public override bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        if (!CanBind(key) || !TryGetAvatarForKey(key, out TAvatar? avatar) || avatar is null)
        {
            return false;
        }

        value = GetValueForAvatar(avatar);

        return true;
    }

    protected abstract WiredVariableValue GetValueForAvatar(TAvatar avatar);

    protected virtual bool TryGetAvatarForKey(in WiredVariableKey key, out TAvatar? avatar)
    {
        avatar = default;

        if (
            !_roomGrain._state.AvatarsByObjectId.TryGetValue(key.TargetId, out IRoomAvatar? found)
            || found is not TAvatar typed
        )
        {
            return false;
        }

        avatar = typed;

        return true;
    }
}
