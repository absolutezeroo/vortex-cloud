using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

public sealed class UserPositionYVariable(RoomGrain roomGrain) : UserPositionVariable(roomGrain)
{
    protected override string VariableName => "@position.y";
    protected override ushort Order => 30;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(avatar.Y);

        return true;
    }

    protected override (int X, int Y) Destination(IRoomAvatar avatar, int value) =>
        (avatar.X, value);
}
