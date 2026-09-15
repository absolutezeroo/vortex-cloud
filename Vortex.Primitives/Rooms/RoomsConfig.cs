namespace Vortex.Primitives.Rooms;

/// <summary>
/// Config keys and defaults for room ownership limits, served live from <c>IServerConfigGrain</c>.
/// The default is the fallback when a key has no admin override stored in the DB.
/// </summary>
/// <remarks>
/// Here rather than beside the handlers that read it, because the limit is not a packet-layer
/// concern: the navigator's "can I create a room" screen asks the question, and the room service is
/// what has to answer it when the room is actually created. It lived only where the screen could see
/// it, and the creation path enforced nothing at all as a result.
/// </remarks>
public static class RoomsConfig
{
    /// <summary>Also doubles as the cap for "list all my rooms" dialogs: a player can never have
    /// more rooms than this, so a page listing them needs no separate limit.</summary>
    public const string MaxRoomsPerPlayerKey = "rooms.max_rooms_per_player";
    public const int MaxRoomsPerPlayerDefault = 50;

    /// <summary>Ceiling on the population limit a room may be created with. The client's own dialog
    /// offers a short list topping out well below this; the packet carries a raw int.</summary>
    public const int MaxPlayersCeiling = 250;

    /// <summary>
    /// Ceiling on a room's stored name and description, applied by truncation on create and on
    /// rename.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both travel from the wire into columns that declare no length, which EF maps to
    /// <c>longtext</c> in MySQL: a room name could be as long as a packet body allows, 64 KB, and
    /// was stored as sent. Nothing in the client, the dashboard or the seeds states an intended
    /// limit, so these numbers are NOT derived from one -- they are taken from the nearest thing
    /// this repository already decided, <c>RoomAdvertisementService</c>, which caps an ad's name at
    /// 100 and its description at 255. Matching a sibling is worth more than a third invented
    /// number, and an operator who disagrees has one place to change.
    /// </para>
    /// <para>
    /// Truncation rather than refusal, deliberately: refusing would make a too-long rename fail the
    /// way a wired box with the wrong rule count fails -- silently, with the dialog closing as if it
    /// had worked. Room tags are already truncated at 25 for the same reason.
    /// </para>
    /// </remarks>
    public const int MaxNameLength = 100;

    /// <inheritdoc cref="MaxNameLength"/>
    public const int MaxDescriptionLength = 255;

    /// <summary>Trims, then caps at <paramref name="maxLength"/>. Null becomes empty.</summary>
    public static string Clamp(string? value, int maxLength)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
