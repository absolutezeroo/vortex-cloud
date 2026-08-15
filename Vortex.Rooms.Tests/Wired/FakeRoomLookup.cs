using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A room holding exactly the avatars a wired test put in it. Hand-written rather than proxied
/// because the lookup's dictionary-shaped members answer through <c>out</c> parameters.
/// </summary>
internal sealed class FakeRoomLookup(params IRoomPlayer[] players) : IRoomLookup
{
    private readonly IRoomPlayer[] _players = players;

    public IReadOnlyCollection<IRoomAvatar> Avatars => _players;

    public IReadOnlyCollection<IRoomItem> Items => [];

    public int AvatarCount => _players.Length;

    public IRoomAvatar? FindAvatarByPlayer(PlayerId playerId) =>
        _players.FirstOrDefault(p => p.PlayerId == playerId);

    public bool TryFindAvatarByPlayer(
        PlayerId playerId,
        [NotNullWhen(true)] out IRoomAvatar? avatar
    )
    {
        avatar = FindAvatarByPlayer(playerId);

        return avatar is not null;
    }

    public IRoomItem? FindItem(RoomObjectId objectId) => null;

    public IRoomAvatar? FindAvatar(RoomObjectId objectId) => null;

    public bool TryFindItem(RoomObjectId objectId, [NotNullWhen(true)] out IRoomItem? item)
    {
        item = null;

        return false;
    }

    public bool TryFindAvatar(RoomObjectId objectId, [NotNullWhen(true)] out IRoomAvatar? avatar)
    {
        avatar = null;

        return false;
    }
}
