using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Serves the in-client furni editor's read. Vortex-specific: no AS3 or Habbo equivalent.
///
/// Silence is the response to an unauthorized or unresolvable request. The editor never opens
/// without this reply, so a caller without <c>room.furni.edit</c> gets no window and no confirmation
/// that the item exists.
/// </summary>
public class VortexGetFurniEditorDataMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IFurnitureDefinitionProvider definitionProvider
) : IMessageHandler<VortexGetFurniEditorDataMessage>
{
    public async ValueTask HandleAsync(
        VortexGetFurniEditorDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.Has(Capabilities.Room.FurniEdit))
        {
            return;
        }

        IRoomFurni roomGrain = grainFactory.GetRoomFurni(ctx.RoomId);

        RoomItemSnapshot? item = await roomGrain
            .GetItemSnapshotByIdAsync(message.ObjectId, ct)
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        await ctx.SendComposerAsync(BuildEditorData(item, definitionProvider), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Shared with the apply handler so a read and a post-edit acknowledgement are byte-identical —
    /// the editor window redraws from either without caring which it received.
    /// </summary>
    internal static VortexFurniEditorDataMessageComposer BuildEditorData(
        RoomItemSnapshot item,
        IFurnitureDefinitionProvider definitionProvider,
        string error = ""
    )
    {
        FurnitureDefinitionSnapshot? definition = definitionProvider.TryGetDefinition(
            item.DefinitionId
        );

        return new VortexFurniEditorDataMessageComposer
        {
            ObjectId = item.ObjectId,
            // The snapshot's concrete type is the authority on floor-vs-wall; the definition lookup
            // can miss (a row pointing at a deleted definition), and this field decides which
            // controls the editor shows.
            ProductType = item is RoomWallItemSnapshot ? ProductType.Wall : ProductType.Floor,
            DefinitionId = item.DefinitionId,
            SpriteId = item.SpriteId,
            DefinitionName = definition?.Name ?? string.Empty,
            X = item.X,
            Y = item.Y,
            ZHundredths = item.Z.ToInt(),
            Rotation = item.Rotation,
            WallOffset = item is RoomWallItemSnapshot wallItem ? wallItem.WallOffset : 0,
            ExtraData = item.ExtraData,
            OwnerId = item.OwnerId,
            OwnerName = item.OwnerName,
            Error = error,
        };
    }
}
