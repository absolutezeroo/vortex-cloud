using System.Collections.Immutable;

namespace Vortex.Primitives.Players.Avatar;

/// <summary>
/// The collections a wearable avatar can belong to.
/// </summary>
/// <remarks>
/// <para>
/// Not a label the hotel invents: the client switches on this exact string to pick both the caption
/// it shows under the avatar and the two-to-four colours it tints the tile with. Anything else falls
/// through its default branch, which returns <c>null</c> for the name — so an unknown collection is
/// not a cosmetic slip, it draws the caption as the literal text "null #12" in white.
/// </para>
/// <para>
/// That is why this is a closed list rather than a free-text column: three values exist, and the
/// dashboard offers those three.
/// </para>
/// </remarks>
public static class NftAvatarCollection
{
    /// <summary>An avatar handed out whole. Orange caption.</summary>
    public const string Avatar = "habbo:avatar";

    /// <summary>Clothing rather than a character. Purple caption.</summary>
    public const string Clothes = "habbo:clothes";

    /// <summary>The crafted tier — the client gives this one a four-colour tile, the others two.</summary>
    public const string Genesis = "habbo:avatar_genesis";

    public static readonly ImmutableArray<string> All = [Avatar, Clothes, Genesis];

    public static bool IsKnown(string collection) => All.Contains(collection);
}
