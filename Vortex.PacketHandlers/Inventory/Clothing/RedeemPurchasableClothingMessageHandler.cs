using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Clothing;
using Vortex.Primitives.Messages.Outgoing.Inventory.Clothing;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.PacketHandlers.Inventory.Clothing;

/// <summary>
/// Binding a clothing furni: it is consumed, and what it carries becomes wearable.
/// </summary>
/// <remarks>
/// <para>
/// Two existing steps rather than a third way of destroying furniture. The room picks the item up —
/// which is already permission-checked, already tells the room, and already audits — and the
/// clothing grain then consumes it out of the inventory it just landed in. If the second step
/// refuses, the player is left holding the furni, which is the right way for this to fail.
/// </para>
/// <para>
/// The answer is the inventory list, not a result code: the client waits up to five seconds for a
/// <c>FigureSetIds</c> naming the classname it sent, and only then applies the outfit it has already
/// drawn. So the list goes out on every path, refusals included — silence would leave it waiting out
/// the window and dropping the change without a word.
/// </para>
/// </remarks>
public class RedeemPurchasableClothingMessageHandler(
    IGrainFactory grainFactory,
    ILogger<RedeemPurchasableClothingMessageHandler> logger
) : IMessageHandler<RedeemPurchasableClothingMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<RedeemPurchasableClothingMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        RedeemPurchasableClothingMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        PlayerId playerId = new(ctx.PlayerId);
        IPlayerClothingGrain clothing = _grainFactory.GetPlayerClothingGrain(playerId);

        bool pickedUp = await _grainFactory
            .GetRoomFurni(ctx.RoomId)
            .RemoveItemByIdAsync(ctx.AsActionContext(), new RoomObjectId(message.RoomObjectId), ct)
            .ConfigureAwait(false);

        ClothingRedeemResult result = pickedUp
            ? await clothing.RedeemAsync(message.RoomObjectId, ct).ConfigureAwait(false)
            : new ClothingRedeemResult
            {
                Outcome = ClothingRedeemOutcome.NotOwned,
                Clothing = await clothing.GetUnlockedAsync(ct).ConfigureAwait(false),
            };

        // Named in the log whatever it was: the client cannot tell one refusal from another, so
        // without this a refused redemption leaves no trace at all.
        _logger.LogInformation(
            "Clothing redemption of object {ObjectId} by player {PlayerId}: {Outcome}.",
            message.RoomObjectId,
            ctx.PlayerId,
            result.Outcome
        );

        await ctx.SendComposerAsync(
                new FigureSetIdsEventMessageComposer
                {
                    FigureSetIds = result.Clothing.FigureSetIds,
                    BoundFurnitureNames = result.Clothing.BoundFurnitureNames,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
