using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// Which of the client's inventories a mintable type is looked up in.
/// </summary>
/// <remarks>
/// <para>
/// <b>This encoding is the reverse of <see cref="CollectibleProductIdentity"/>'s.</b> There,
/// <c>productTypeId</c> is 0 for a wall item and 1 for a floor item. Here the client maps the short
/// it reads to its legacy letters — 0 to <c>"s"</c>, 1 to <c>"i"</c>, 2 to <c>"cl"</c> — and then
/// asks its furniture model for a group item with <c>isWallItem == (letter == "i")</c>. So 0 is a
/// floor item and 1 is a wall item, the opposite way round, in two messages of the same tab.
/// </para>
/// <para>
/// Getting it wrong is silent in the worst way: the client looks the sprite id up in the other
/// inventory, finds nothing, and shows the item as one the player owns none of — so the mint button
/// stays greyed out and there is nothing on screen to say why.
/// </para>
/// <para>
/// That the numbers happen to line up with <see cref="ProductType"/>'s own Floor=0/Wall=1 is a
/// coincidence, and only holds for those two: the client's 2 means clothes, where
/// <see cref="ProductType"/>'s 2 means an effect.
/// </para>
/// </remarks>
public static class MintableItemKind
{
    /// <summary>The client's <c>"s"</c>.</summary>
    public const short Floor = 0;

    /// <summary>The client's <c>"i"</c>.</summary>
    public const short Wall = 1;

    /// <summary>The client's <c>"cl"</c>. No furniture definition maps here — clothes are not
    /// furniture — so nothing produces it yet.</summary>
    public const short Clothes = 2;

    /// <summary>Which inventory the client should count this furniture in.</summary>
    public static short ForFurniture(ProductType productType) =>
        productType == ProductType.Wall ? Wall : Floor;
}
