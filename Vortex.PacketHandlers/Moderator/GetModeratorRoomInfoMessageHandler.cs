using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Moderator;
using Vortex.Protocol.Messages.Outgoing.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// Backs the mod tool's room-tool panel. The moderator is usually not standing in the room they are
/// looking up, so everything is read straight off the target room's grain rather than off
/// <c>ctx.RoomId</c>.
/// </summary>
public class GetModeratorRoomInfoMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<GetModeratorRoomInfoMessage>
{
    public async ValueTask HandleAsync(
        GetModeratorRoomInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.Has(Capabilities.Room.ModerateAny))
        {
            return;
        }

        RoomId roomId = message.RoomId;

        // The room tool's caution/message buttons carry no room id, so this request is the only
        // place the server learns which room they will mean. See ModToolRoomAlertMessage.
        await grainFactory
            .GetModerationQueueGrain()
            .NoteInspectedRoomAsync(PlayerId.Parse(ctx.PlayerId), roomId.Value)
            .ConfigureAwait(false);

        RoomSummarySnapshot summary = await grainFactory
            .GetRoomCore(roomId)
            .GetSummaryAsync()
            .ConfigureAwait(false);

        // A never-persisted room id activates a grain that reports id -1; that is the signal the
        // client's optional room block exists for.
        bool roomExists = summary.RoomId > 0;

        ImmutableArray<RoomAvatarSnapshot> avatars = await grainFactory
            .GetRoomAvatars(roomId)
            .GetAllAvatarSnapshotsAsync(ct)
            .ConfigureAwait(false);

        bool ownerInRoom = avatars.Any(avatar =>
            avatar.AvatarType == RoomObjectType.Player && avatar.WebId == summary.OwnerId
        );

        await ctx.SendComposerAsync(
                new ModeratorRoomInfoEventMessageComposer
                {
                    RoomId = roomId,
                    UserCount = summary.Population,
                    OwnerInRoom = ownerInRoom,
                    OwnerId = summary.OwnerId,
                    OwnerName = summary.OwnerName,
                    RoomExists = roomExists,
                    RoomName = summary.Name,
                    RoomDescription = summary.Description,
                    // Tags are not part of the room summary and the mod tool only uses them for
                    // display, so the list goes out empty rather than costing a second grain call.
                    Tags = ImmutableArray<string>.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
