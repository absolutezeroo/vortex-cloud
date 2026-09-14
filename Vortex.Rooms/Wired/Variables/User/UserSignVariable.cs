using System.Globalization;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Which hand sign the avatar is holding up, 0 when none. The sign is a status the avatar tick
/// retires after a few seconds, so a chain that reads this is reading a moment — which is what makes
/// it worth reading at all.
/// </summary>
public sealed class UserSignVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@sign";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 160;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value =
            avatar.Statuses.TryGetValue(AvatarStatusType.Sign, out string? signId)
            && int.TryParse(signId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
                ? WiredVariableValue.Parse(id)
                : WiredVariableValue.Parse(0);

        return true;
    }
}
