using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// One coordinate of where the avatar is standing, readable and writable.
/// </summary>
/// <remarks>
/// Writing one moves the avatar to the tile the pair names, as a slide rather than a teleport, which
/// is what <c>wf_act_move_user</c> does and what the client is prepared to animate.
/// <para>
/// The destination is checked against the map first and a blocked or out-of-room tile is refused. A
/// wired box cannot be allowed to put someone inside a wall or off the floor plan: the avatar would
/// be standing on a tile the pathfinder will not walk out of, and only a re-entry would free them.
/// </para>
/// </remarks>
public abstract class UserPositionVariable(RoomGrain roomGrain)
    : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Position;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable;

    /// <summary>Where the avatar ends up when this coordinate becomes <paramref name="value"/>, the
    /// other one staying as it is.</summary>
    protected abstract (int X, int Y) Destination(IRoomAvatar avatar, int value);

    protected override async Task<bool> SetValueForAvatarAsync(
        IWiredExecutionContext ctx,
        IRoomAvatar avatar,
        WiredVariableValue value
    )
    {
        (int x, int y) = Destination(avatar, value);
        int destination = _roomGrain.MapModule.ToIdx(x, y);

        if (x < 0 || y < 0 || !_roomGrain.MapModule.CanAvatarWalk(avatar, destination))
        {
            return false;
        }

        await ctx.ProcessUserMovementAsync(avatar, destination, SlideAvatarMoveType.Move);

        return true;
    }
}
