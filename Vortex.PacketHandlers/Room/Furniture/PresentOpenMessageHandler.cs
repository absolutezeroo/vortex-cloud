using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Unwraps a present: the room consumes the parcel, the catalogue grants what was inside, and the
/// client is told what appeared so it can show it.
/// </summary>
public class PresentOpenMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PresentOpenMessage>
{
    public async ValueTask HandleAsync(
        PresentOpenMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        PresentContentsSnapshot? contents = await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .OpenPresentAsync(ctx.AsActionContext(), message.ObjectId, ct)
            .ConfigureAwait(false);

        if (contents is null)
        {
            return;
        }

        CatalogOfferSnapshot? offer = await grainFactory
            .GetCatalogPurchaseGrain(ctx.PlayerId)
            .GrantPresentContentsAsync(contents.OfferId, contents.ExtraParam, ct)
            .ConfigureAwait(false);

        if (offer is null || offer.Products.Length == 0)
        {
            return;
        }

        CatalogProductSnapshot product = offer.Products[0];

        // PlacedInRoom is false and PlacedItemId zero on purpose: the contents go to the opener's
        // inventory, not onto the floor. Habbo only fills those in for the gifts it auto-places,
        // and a client told an item was placed draws it in a room where nothing exists.
        await ctx.SendComposerAsync(
                new PresentOpenedMessageComposer
                {
                    ItemType = product.ProductType == ProductType.Floor ? "S" : "I",
                    ClassId = product.FurniDefinitionId,
                    ProductCode = product.ProductType,
                    PlacedItemId = 0,
                    PlacedItemType = product.ProductType,
                    PlacedInRoom = false,
                    PetFigureString = string.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
