namespace Vortex.Primitives.Groups;

/// <summary>
/// The single definition of what makes a guild name acceptable. Shared by the creation path (via
/// the cancellable <c>GroupCreatingEvent</c> behavior) and the rename path, so a name that cannot
/// be used to create a guild cannot be reached by renaming one either.
/// </summary>
public static class GroupNameRules
{
    /// <summary>Cancel reason for a name that is empty or whitespace only.</summary>
    public const string EmptyNameReason = "empty_name";

    /// <summary>Cancel reason for a name longer than the configured maximum.</summary>
    public const string NameTooLongReason = "name_too_long";

    /// <summary>
    /// Returns null when <paramref name="name"/> is acceptable, otherwise the matching reason code.
    /// </summary>
    public static string? Validate(string? name, int maxLength)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return EmptyNameReason;
        }

        return trimmed.Length > maxLength ? NameTooLongReason : null;
    }
}
