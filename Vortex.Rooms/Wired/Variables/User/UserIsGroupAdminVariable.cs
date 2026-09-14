using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Whether the avatar is an admin of the guild that owns this room. A player with no rank in that
/// guild — or a room no guild owns — is simply not one, so the reading does not resolve.
/// </summary>
public sealed class UserIsGroupAdminVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@is_group_admin";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 60;
    protected override WiredVariableFlags Flags => WiredVariableFlags.None;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        return _roomGrain._state.GroupMemberRanks.TryGetValue(
                avatar.PlayerId,
                out GroupMemberRank rank
            )
            && rank == GroupMemberRank.Admin;
    }
}
