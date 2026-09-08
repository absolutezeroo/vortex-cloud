namespace Vortex.Primitives.Groups.Enums;

/// <summary>
/// What a hotel operator can do to one forum item, from outside the guild.
/// </summary>
/// <remarks>
/// A guild's own moderators act through <c>ModerateThreadAsync</c>/<c>ModerateMessageAsync</c>, which
/// are gated on the forum's permission settings. An operator is not a member and has no rank in the
/// guild, so those gates would refuse them -- and the content they are asked to act on is exactly
/// the content the guild's own moderators are unwilling or unable to remove.
/// <para>
/// <see cref="Hide"/> is what the client already understands: a hidden thread or post disappears from
/// every read and can be put back. <see cref="Delete"/> marks the row deleted, which every forum
/// query filters on, and is what an operator means when hiding is not enough.
/// </para>
/// </remarks>
public enum ForumStaffAction
{
    Hide = 0,
    Restore = 1,
    Delete = 2,
}
