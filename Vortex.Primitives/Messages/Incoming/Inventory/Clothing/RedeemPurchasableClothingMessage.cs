using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Clothing;

/// <summary>
/// Binding a clothing furni to the account: it is consumed, and the figure sets it carries become
/// wearable.
/// </summary>
/// <remarks>
/// <para>
/// The furni is named by its <em>room-object</em> id, not an inventory id — the player redeems it by
/// clicking it where it stands, from the furniture context menu.
/// </para>
/// <para>
/// There is no result message. The client waits for a <c>FigureSetIds</c> whose bound-furniture list
/// contains the classname it just sent, for five seconds, and only then applies the outfit it has
/// already previewed. Past that it gives up without a word, so the answer has to be prompt.
/// </para>
/// </remarks>
public record RedeemPurchasableClothingMessage : IMessageEvent
{
    public required int RoomObjectId { get; init; }
}
