using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Database.Entities.Groups;

/// <summary>
/// Forum configuration for a group. Modelled as a 1:1 table (not columns on
/// <see cref="GroupEntity"/>) per DATA-MODEL §2.4. Invariant: exactly one row per group,
/// created with its defaults at group creation (see <see cref="GroupFactory"/>).
/// </summary>
[Table("group_forum_settings")]
[Index(nameof(GroupEntityId), IsUnique = true)]
public class GroupForumSettingsEntity : VortexEntity
{
    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("enabled")]
    [DefaultValue(GroupFactory.DefaultForumEnabled)]
    public required bool Enabled { get; set; }

    [Column("read_permission")]
    [DefaultValue(GroupFactory.DefaultReadPermission)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupForumPermission ReadPermission { get; set; }

    [Column("post_permission")]
    [DefaultValue(GroupFactory.DefaultPostPermission)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupForumPermission PostPermission { get; set; }

    [Column("thread_permission")]
    [DefaultValue(GroupFactory.DefaultThreadPermission)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupForumPermission ThreadPermission { get; set; }

    [Column("mod_permission")]
    [DefaultValue(GroupFactory.DefaultModPermission)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupForumPermission ModPermission { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }
}
