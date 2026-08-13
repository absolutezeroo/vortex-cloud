namespace Vortex.Primitives.Collectibles;

/// <summary>
/// Whether a collection is shown to players.
/// </summary>
/// <remarks>
/// <para>
/// The client parses this field and exposes a getter for it, but nothing anywhere in its
/// collectibles interface reads that getter — so the value on the wire changes nothing on screen,
/// and these meanings are this hotel's own rather than Sulake's. That is precisely why the server
/// has to act on them: an admin setting a collection to Draft expects it to be hidden, and the
/// client will not do it for us.
/// </para>
/// <para>
/// The three values match the count of constants the client declares, so a hotel that later
/// discovers what Sulake meant by them can reinterpret without a migration.
/// </para>
/// </remarks>
public static class NftCollectionStatus
{
    /// <summary>Being built. Never sent to players, so items can be added over several sittings
    /// without a half-finished set appearing in anyone's Collectors Guild.</summary>
    public const int Draft = 0;

    /// <summary>Live: this is the only status players ever see.</summary>
    public const int Visible = 1;

    /// <summary>Retired. Kept for its history and its scores, but out of the guild.</summary>
    public const int Archived = 2;

    /// <summary>The one test that decides whether players see a collection at all.</summary>
    public static bool IsVisibleToPlayers(int status) => status == Visible;
}
