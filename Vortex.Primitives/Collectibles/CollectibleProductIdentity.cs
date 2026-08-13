using System.Globalization;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// How the client is told which picture to draw for a collectible.
/// </summary>
/// <remarks>
/// <para>
/// Two fields decide it, and neither is what its name suggests. <c>productTypeId</c> chooses which
/// of the client's furniture tables to search — <see cref="Wall"/> and <see cref="Floor"/> for
/// furniture — and <c>itemTypeId</c> is read with <c>parseInt</c>, so it must be the definition's
/// sprite id rather than its classname.
/// </para>
/// <para>
/// Getting either wrong does not fail: the client draws whatever it does find. Sending the classname
/// as the item type is how a dragon lamp came out as a post-it, because
/// <c>parseInt("02_dragonlamp_skream")</c> is 2 and sprite 2 is the post-it. Both values are
/// therefore derived from the furniture definition here, in one place, rather than stored or typed.
/// </para>
/// </remarks>
public static class CollectibleProductIdentity
{
    public const int Wall = 0;
    public const int Floor = 1;

    /// <summary>Pets and clothes are listed under their own shop headings, and pets are drawn from a
    /// figure string instead of a sprite. Named here because the numbers are otherwise unreadable.</summary>
    public const int Pet = 10;
    public const int Clothes = 11;

    /// <summary>Which table the client should search for this piece of furniture.</summary>
    public static int ForFurniture(ProductType productType) =>
        productType == ProductType.Wall ? Wall : Floor;

    /// <summary>The sprite id as the client reads it: a number in a string.</summary>
    public static string ItemTypeId(int spriteId) =>
        spriteId.ToString(CultureInfo.InvariantCulture);
}
