using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

public sealed class UserPositionXVariable(RoomGrain roomGrain) : UserPositionVariable(roomGrain)
{
    protected override string VariableName => "@position.x";
    protected override ushort Order => 20;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(avatar.X);

        return true;
    }

    protected override (int X, int Y) Destination(IRoomAvatar avatar, int value) =>
        (value, avatar.Y);
}
