using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired;
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

    /// <summary>
    /// The guard every write shares, in one place: the variable must say it is writable, the key must
    /// be this variable's, and the avatar must still be in the room.
    /// </summary>
    /// <remarks>
    /// The flag test is not ceremony. It is what the "change variable value" box filters its picker
    /// on, so a variable that declares it and does nothing is offered to the builder and then silently
    /// ignores them — which is exactly what <c>@type</c> and <c>@position.x</c> did.
    /// </remarks>
    public override async Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        if (
            !GetVarSnapshot().Flags.Has(WiredVariableFlags.CanWriteValue)
            || !CanBind(key)
            || !TryGetAvatarForKey(key, out TAvatar? avatar)
            || avatar is null
        )
        {
            return false;
        }

        return await SetValueForAvatarAsync(ctx, avatar, value);
    }

    /// <summary>
    /// What this variable does to the avatar when a chain writes to it. The default refuses, which is
    /// the honest answer for a reading nothing can set — and a leaf that declares
    /// <see cref="WiredVariableFlags.CanWriteValue"/> without overriding this is a leaf that lies to
    /// the picker.
    /// </summary>
    protected virtual Task<bool> SetValueForAvatarAsync(
        IWiredExecutionContext ctx,
        TAvatar avatar,
        WiredVariableValue value
    ) => Task.FromResult(false);

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
