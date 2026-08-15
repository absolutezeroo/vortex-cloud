using System.Globalization;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Which guild the two group-aware wired boxes ("actor is a group member" and the "users in group"
/// selector) are asking about.
/// </summary>
/// <remarks>
/// Both boxes carry the same two-option form in the client (UsersInGroup.as and the condition that
/// mirrors it): a radio group whose first option is "Current group" and whose second picks one of
/// the configuring player's own guilds from a dropdown. Only the second option writes anything —
/// <c>readStringParamFromForm()</c> returns the guild id as a decimal string, and the empty string
/// for "Current group". There is no int param at all, so the empty string is the whole of the
/// first option's configuration and must be read as "whichever guild this room belongs to".
/// </remarks>
public static class WiredGroupTarget
{
    /// <summary>
    /// The guild id the box is configured against, or null when it resolves to nothing — an empty
    /// param in a room with no guild, or a param the client never should have sent. A null target
    /// makes the condition fail rather than match everyone.
    /// </summary>
    public static int? Resolve(string? stringParam, int? roomGroupId)
    {
        string trimmed = (stringParam ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            return roomGroupId > 0 ? roomGroupId : null;
        }

        if (
            !int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            || id <= 0
        )
        {
            return null;
        }

        return id;
    }
}
