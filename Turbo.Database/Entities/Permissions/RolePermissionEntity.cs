using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Permissions;

/// <summary>Grants a single capability key (see <c>Turbo.Primitives.Permissions.Capabilities</c>) to
/// a role.</summary>
[Table("role_permissions")]
[Index(nameof(RoleEntityId), nameof(CapabilityKey), IsUnique = true)]
public class RolePermissionEntity : TurboEntity
{
    [Column("role_id")]
    public required int RoleEntityId { get; set; }

    [Column("capability_key")]
    [MaxLength(128)]
    public required string CapabilityKey { get; set; }

    [ForeignKey(nameof(RoleEntityId))]
    public RoleEntity? RoleEntity { get; set; }
}
