using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Which way the body faces, 0–7 clockwise from north. The head can be turned separately and is not
/// this reading: an avatar looking over its shoulder is still walking the way its body points, which
/// is the direction a builder means.
/// </summary>
public sealed class UserDirectionVariable(RoomGrain roomGrain)
    : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@direction";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 80;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = (int)avatar.Rotation;

        return true;
    }

    /// <summary>
    /// Turns the whole avatar, head with body, which is what the <c>wf_act_move_user</c> box's rotate
    /// half does. A value outside 0–7 is refused rather than wrapped: the eight directions are the
    /// whole of the space, so 9 is a mistake and hiding it as 1 would only make it harder to find.
    /// </summary>
    protected override async Task<bool> SetValueForAvatarAsync(
        IWiredExecutionContext ctx,
        IRoomAvatar avatar,
        WiredVariableValue value
    )
    {
        if (value < 0 || value > 7)
        {
            return false;
        }

        await ctx.ProcessUserDirectionAsync(avatar, (Rotation)value.Value, (Rotation)value.Value);

        return true;
    }
}
