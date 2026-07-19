using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Permissions;

/// <summary>A named bundle of capabilities. Accounts are assigned roles; their effective rights are
/// the union of their roles' capabilities.</summary>
[Table("roles")]
[Index(nameof(Key), IsUnique = true)]
public class RoleEntity : VortexEntity
{
    [Column("key")]
    [MaxLength(64)]
    public required string Key { get; set; }

    [Column("name")]
    [MaxLength(128)]
    public required string Name { get; set; }

    [InverseProperty(nameof(RolePermissionEntity.RoleEntity))]
    public List<RolePermissionEntity>? Permissions { get; set; }

    [InverseProperty(nameof(PlayerAccountRoleEntity.RoleEntity))]
    public List<PlayerAccountRoleEntity>? PlayerAccountRoles { get; set; }
}
