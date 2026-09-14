using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// The achievement score the room was told at entry. It is held on the avatar as
/// <c>ActivityPoints</c> — the same field the room hands back as <c>AchievementScore</c> on every
/// user-change broadcast — so this reading and the number over the avatar's head can never disagree.
/// <para>
/// It is a snapshot taken when the player walked in, not a live account read: an achievement earned
/// elsewhere in the hotel reaches it on the next entry.
/// </para>
/// </summary>
public sealed class UserAchievementScoreVariable(RoomGrain roomGrain)
    : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@achievement_score";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 40;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(avatar.ActivityPoints);

        return true;
    }
}
