using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

public sealed class UserPositionXVariable(RoomGrain roomGrain)
    : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@position.x";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Position;
    protected override ushort Order => 20;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForAvatar(IRoomAvatar avatar) =>
        WiredVariableValue.Parse(avatar.X);
}
