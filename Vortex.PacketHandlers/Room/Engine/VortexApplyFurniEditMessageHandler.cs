using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Protocol.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Applies an in-client furni-editor edit. Vortex-specific: no AS3 or Habbo equivalent.
///
/// Re-resolves <c>room.furni.edit</c> on every request — the handshake-time rights flag the client
/// holds decides whether a button is drawn, never whether a write is allowed.
/// </summary>
public class VortexApplyFurniEditMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IFurnitureDefinitionProvider definitionProvider
) : IMessageHandler<VortexApplyFurniEditMessage>
{
    public async ValueTask HandleAsync(
        VortexApplyFurniEditMessage message,
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

        // The two lookups the grain cannot do itself. Resolved before the call so a bad username or
        // a bad definition id fails without the grain having mutated anything.
        PlayerId? newOwnerId = null;
        string? newOwnerName = null;

        if (message.Fields.HasFlag(FurniEditField.Owner))
        {
            newOwnerId = await grainFactory
                .GetPlayerDirectoryGrain()
                .GetPlayerIdAsync(message.OwnerName, ct)
                .ConfigureAwait(false);

            if (newOwnerId is null || newOwnerId.Value == PlayerId.Invalid)
            {
                await RespondAsync(roomGrain, ctx, message, "owner_not_found", ct)
                    .ConfigureAwait(false);

                return;
            }

            newOwnerName = message.OwnerName;
        }

        FurnitureDefinitionSnapshot? newDefinition = null;

        if (message.Fields.HasFlag(FurniEditField.Definition))
        {
            newDefinition = definitionProvider.TryGetDefinition(message.DefinitionId);

            if (newDefinition is null)
            {
                await RespondAsync(roomGrain, ctx, message, "definition_not_found", ct)
                    .ConfigureAwait(false);

                return;
            }
        }

        string error = await roomGrain
            .ApplyFurniEditAsync(
                ctx.AsActionContext(),
                new FurniEditRequest
                {
                    ObjectId = message.ObjectId,
                    Fields = message.Fields,
                    X = message.X,
                    Y = message.Y,
                    ZHundredths = message.ZHundredths,
                    Rotation = message.Rotation,
                    WallOffset = message.WallOffset,
                    ExtraData = message.ExtraData,
                    DefinitionId = message.DefinitionId,
                },
                newOwnerId,
                newOwnerName,
                newDefinition,
                ct
            )
            .ConfigureAwait(false);

        await RespondAsync(roomGrain, ctx, message, error, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Echoes the item's post-edit server state back to the editor, carrying <paramref name="error"/>
    /// when the edit was refused. Sent on success too: the room-wide update composers tell every
    /// client what to draw, but they do not carry owner, definition or altitude at the precision the
    /// editor's own fields need, so the window would otherwise keep showing what was typed rather
    /// than what was stored.
    /// </summary>
    private async Task RespondAsync(
        IRoomFurni roomGrain,
        MessageContext ctx,
        VortexApplyFurniEditMessage message,
        string error,
        CancellationToken ct
    )
    {
        RoomItemSnapshot? item = await roomGrain
            .GetItemSnapshotByIdAsync(message.ObjectId, ct)
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                VortexGetFurniEditorDataMessageHandler.BuildEditorData(
                    item,
                    definitionProvider,
                    error
                ),
                ct
            )
            .ConfigureAwait(false);
    }
}
