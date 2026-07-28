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

        // UpdateWithPlayer also stamps the favourite-guild badge from the snapshot, so the avatar
        // enters the room already wearing it rather than staying unbadged until a manual toggle.
        avatar.UpdateWithPlayer(snapshot);

        return avatar;
    }
}
