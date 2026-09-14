using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Whether the avatar is silenced. It asks the chat system the same question the chat path asks, so
/// both mutes count — this room's own and the hotel-wide sanction the player carries between rooms —
/// and a chain can never think someone may speak when the next line they type would be dropped.
/// </summary>
public sealed class UserIsMutedVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@is_muted";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 120;
    protected override WiredVariableFlags Flags => WiredVariableFlags.None;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        return _roomGrain.ChatSystem.IsUserMuted(avatar.PlayerId, out _);
    }
}
