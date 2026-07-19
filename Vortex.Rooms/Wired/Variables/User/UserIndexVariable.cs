using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

public sealed class UserIndexVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@index";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 20;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override WiredVariableValue GetValueForAvatar(IRoomAvatar avatar) =>
        WiredVariableValue.Parse(avatar.ObjectId);
}
