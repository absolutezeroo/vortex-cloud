using Turbo.Primitives.Groups.Enums;

namespace Turbo.Database.Entities.Groups;

/// <summary>
/// Single creation path for a group. Enforces the DATA-MODEL §2.4 invariant: every group is
/// created together with exactly one <see cref="GroupForumSettingsEntity"/> row carrying the
/// forum defaults. Build the graph here and attach it to the context (EF inserts the group and
/// its settings row atomically through the <see cref="GroupEntity.ForumSettings"/> navigation).
/// </summary>
public static class GroupFactory
{
    // Forum defaults applied at group creation. A freshly created group starts with its forum
    // turned off (the owner opts in), members may read/post/open threads, and admins moderate.
    public const bool DefaultForumEnabled = false;
    public const GroupForumPermission DefaultReadPermission = GroupForumPermission.Members;
    public const GroupForumPermission DefaultPostPermission = GroupForumPermission.Members;
    public const GroupForumPermission DefaultThreadPermission = GroupForumPermission.Members;
    public const GroupForumPermission DefaultModPermission = GroupForumPermission.Admins;

    /// <summary>
    /// Creates a new <see cref="GroupEntity"/> with its 1:1 default forum-settings row attached.
    /// The caller adds the returned entity to the context and saves.
    /// </summary>
    public static GroupEntity Create(
        string name,
        string badge,
        int roomEntityId,
        int ownerPlayerEntityId,
        GroupType type,
        string colorOne,
        string colorTwo,
        string? description = null,
        bool adminOnlyDecoration = false
    )
    {
        var group = new GroupEntity
        {
            Name = name,
            Description = description,
            Badge = badge,
            RoomEntityId = roomEntityId,
            OwnerPlayerEntityId = ownerPlayerEntityId,
            Type = type,
            ColorOne = colorOne,
            ColorTwo = colorTwo,
            AdminOnlyDecoration = adminOnlyDecoration,
            RoomEntity = null!,
            OwnerPlayerEntity = null!,
        };

        group.ForumSettings = CreateDefaultForumSettings(group);

        return group;
    }

    /// <summary>
    /// Builds the default forum-settings row for a group (DATA-MODEL §2.4). The
    /// <see cref="GroupForumSettingsEntity.GroupEntity"/> back-reference wires the 1:1 link so EF
    /// resolves <c>group_id</c> from the parent group on insert.
    /// </summary>
    public static GroupForumSettingsEntity CreateDefaultForumSettings(GroupEntity group) =>
        new()
        {
            GroupEntityId = group.Id,
            Enabled = DefaultForumEnabled,
            ReadPermission = DefaultReadPermission,
            PostPermission = DefaultPostPermission,
            ThreadPermission = DefaultThreadPermission,
            ModPermission = DefaultModPermission,
            GroupEntity = group,
        };
}
