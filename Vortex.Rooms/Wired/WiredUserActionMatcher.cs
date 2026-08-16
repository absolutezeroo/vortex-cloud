using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Whether an avatar is doing the thing a wired box asks about, from the client's
/// <c>WiredUserAction</c> code.
/// </summary>
/// <remarks>
/// Shared by the condition ("the user performs this action") and the selector ("the users
/// performing this action"), which are the same question asked of one avatar or of the room.
/// <para>
/// Only durable states can be answered: sitting, lying, standing, waving, holding a sign and
/// dancing are things an avatar *is* doing. The momentary expressions — blow, laugh, respect — and
/// idle sleep leave no state to read, so they report false rather than guessing.
/// </para>
/// </remarks>
public static class WiredUserActionMatcher
{
    public static bool Matches(int actionCode, IRoomPlayer player) =>
        actionCode switch
        {
            0 => player.HasStatus(AvatarStatusType.Wave),
            6 => player.HasStatus(AvatarStatusType.Sit),
            // "Standing" is the absence of the two postures, not a status of its own.
            7 => !player.HasStatus(AvatarStatusType.Sit, AvatarStatusType.Lay),
            8 => player.HasStatus(AvatarStatusType.Lay),
            10 => player.HasStatus(AvatarStatusType.Sign),
            11 => player.DanceType != AvatarDanceType.None,
            _ => false,
        };
}
