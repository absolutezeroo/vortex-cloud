using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// The effect the avatar is wearing, 0 when none. Writing it is the same operation the
/// <c>wf_act_give_effect</c> box performs, taken through the avatar module so the broadcast and the
/// late-joiner re-sync come with it.
/// </summary>
public sealed class UserEffectVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@effect";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 100;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(avatar.CurrentEffectId);

        return true;
    }

    protected override Task<bool> SetValueForAvatarAsync(
        IWiredExecutionContext ctx,
        IRoomAvatar avatar,
        WiredVariableValue value
    ) =>
        value < 0
            ? Task.FromResult(false)
            : _roomGrain.AvatarModule.SetAvatarEffectAsync(
                avatar.ObjectId,
                value,
                CancellationToken.None
            );
}
