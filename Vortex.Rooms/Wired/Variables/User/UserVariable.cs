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

        return TryGetValueForAvatar(avatar, out value);
    }

    /// <summary>
    /// The one reading this variable takes of the avatar. Returning false is how a boolean reading is
    /// spelled on this band — the same shape <c>FurnitureVariable</c> uses, where a flagless variable
    /// answers "true" by resolving at all and "false" by not resolving.
    /// </summary>
    protected abstract bool TryGetValueForAvatar(TAvatar avatar, out WiredVariableValue value);

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
