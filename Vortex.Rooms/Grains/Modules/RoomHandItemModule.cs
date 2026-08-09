using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Grains.Modules;

/// <summary>
/// Hand items: the drink or flower an avatar holds for a while. Nothing here is persisted, on
/// purpose — a hand item is shown and then gone, so a player who reconnects is empty-handed, the
/// same as in Habbo.
/// <para>
/// Passing one is a move rather than a copy: the giver's hand empties as the taker's fills, or the
/// pass is refused. Two people holding the same drink is the bug this avoids.
/// </para>
/// </summary>
public sealed class RoomHandItemModule(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    /// <summary>
    /// Puts an item in a player's hand for the room's configured while. False when they are not
    /// here, which is what a wired stack naming somebody who has left looks like.
    /// </summary>
    public bool Give(PlayerId playerId, int itemId)
    {
        if (itemId <= 0 || !TryFindAvatar(playerId, out IRoomAvatar? avatar))
        {
            return false;
        }

        long untilMs = _roomGrain.NowMs() + _roomGrain._roomConfig.HandItemDurationMs;

        // Re-handing the same item returns false from the avatar because nothing about it changed,
        // but the hand was still refilled and the client still has to be told, or the drink winks
        // out at the original expiry.
        avatar.SetCarryItem(itemId, untilMs);

        Broadcast(avatar.ObjectId, itemId);

        return true;
    }

    /// <summary>Empties a player's hand. False when there was nothing in it.</summary>
    public bool Drop(PlayerId playerId)
    {
        if (!TryFindAvatar(playerId, out IRoomAvatar? avatar) || avatar.CarryItemId == 0)
        {
            return false;
        }

        avatar.SetCarryItem(0, 0);

        Broadcast(avatar.ObjectId, 0);

        return true;
    }

    /// <summary>
    /// Hands what one player is holding to another standing beside them. Refused across a room:
    /// the client only offers the button on somebody within reach, and honouring it at any distance
    /// would let an item be passed to a stranger on the far side of the floor.
    /// </summary>
    public bool Pass(PlayerId fromPlayerId, PlayerId toPlayerId)
    {
        if (
            fromPlayerId == toPlayerId
            || !TryFindAvatar(fromPlayerId, out IRoomAvatar? giver)
            || !TryFindAvatar(toPlayerId, out IRoomAvatar? taker)
            || giver.CarryItemId == 0
            || !IsWithinReach(giver, taker)
        )
        {
            return false;
        }

        int itemId = giver.CarryItemId;

        // The taker gets the full while rather than what was left of the giver's, which is what
        // makes passing a drink round a room work at all.
        long untilMs = _roomGrain.NowMs() + _roomGrain._roomConfig.HandItemDurationMs;

        giver.SetCarryItem(0, 0);
        taker.SetCarryItem(itemId, untilMs);

        Broadcast(giver.ObjectId, 0);
        Broadcast(taker.ObjectId, itemId);

        return true;
    }

    /// <summary>What a player is holding, or zero.</summary>
    public int HeldBy(PlayerId playerId) =>
        TryFindAvatar(playerId, out IRoomAvatar? avatar) ? avatar.CarryItemId : 0;

    /// <summary>
    /// Reach is one tile in any direction, which is the client's own rule for offering the pass
    /// and the give-to-pet buttons.
    /// </summary>
    public static bool IsWithinReach(int fromX, int fromY, int toX, int toY) =>
        Math.Abs(fromX - toX) <= 1 && Math.Abs(fromY - toY) <= 1;

    private static bool IsWithinReach(IRoomAvatar from, IRoomAvatar to) =>
        IsWithinReach(from.X, from.Y, to.X, to.Y);

    private bool TryFindAvatar(PlayerId playerId, [NotNullWhen(true)] out IRoomAvatar? avatar)
    {
        avatar = null;

        return _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
    }

    private void Broadcast(RoomObjectId objectId, int itemId) =>
        _roomGrain
            .SendComposerToRoomAsync(
                new CarryObjectMessageComposer { UserId = objectId.Value, ItemType = itemId }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish a hand item in room {RoomId}",
                _roomGrain._state.RoomId
            );
}
