using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Whether the avatar can build here. Answered by the security module's own owner-or-rights test
/// rather than by reading the rights set directly, so a chain and the room agree on who may place
/// furniture — the owner has rights without appearing in that set.
/// </summary>
public sealed class UserHasRightsVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@has_rights";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 50;
    protected override WiredVariableFlags Flags => WiredVariableFlags.None;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        return _roomGrain.SecurityModule.HasExplicitRights(avatar.PlayerId);
    }
}
