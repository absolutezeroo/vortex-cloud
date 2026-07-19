using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Rooms.Object.Avatars.Player;

namespace Vortex.Rooms.Providers;

public sealed class RoomAvatarProvider : IRoomAvatarProvider
{
    public IRoomPlayer CreateAvatarFromPlayerSnapshot(
        RoomObjectId objectId,
        PlayerSummarySnapshot snapshot
    )
    {
        RoomPlayerAvatar avatar = new RoomPlayerAvatar
        {
            ObjectId = objectId,
            PlayerId = snapshot.PlayerId,
        };

        avatar.UpdateWithPlayer(snapshot);

        return avatar;
    }
}
