using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userclassification;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.PacketHandlers.UserClassification;

/// <summary>
/// The staff <c>:uc &lt;classification&gt;</c> and <c>:anew</c> chat commands, scoped to the room
/// the sender is standing in.
/// </summary>
public class RoomUsersClassificationMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IUserClassificationService classifications
) : IMessageHandler<RoomUsersClassificationMessage>
{
    public async ValueTask HandleAsync(
        RoomUsersClassificationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        RoomId roomId = ctx.RoomId;

        ImmutableArray<RoomAvatarSnapshot> avatars = await grainFactory
            .GetRoomAvatars(roomId)
            .GetAllAvatarSnapshotsAsync(ct)
            .ConfigureAwait(false);

        // Bots and pets share the avatar list and have no account behind them to classify.
        int[] playerIds =
        [
            .. avatars
                .Where(a => a.AvatarType == RoomObjectType.Player && a.WebId > 0)
                .Select(a => a.WebId)
                .Distinct(),
        ];

        await UserClassificationDispatch
            .RespondAsync(
                grainFactory,
                permissionService,
                classifications,
                ctx,
                playerIds,
                message.Classification,
                ct
            )
            .ConfigureAwait(false);
    }
}
