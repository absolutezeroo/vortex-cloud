using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// How high the avatar is standing, in the same whole-number scale <c>@altitude</c> uses on the furni
/// band — <see cref="Vortex.Primitives.Rooms.Object.Altitude"/> keeps fractions the client never
/// sees, so a stack of half-height rugs reads as the step a builder can compare against.
/// </summary>
public sealed class UserAltitudeVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@altitude";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Position;
    protected override ushort Order => 10;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = avatar.Z.ToInt();

        return true;
    }
}
