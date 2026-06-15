using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;

namespace Turbo.Database.Entities.Permissions;

/// <summary>Assigns a role to a player account. Roles attach to the account, so every avatar under it
/// shares the same staff rights.</summary>
[Table("player_account_roles")]
[Index(nameof(PlayerAccountEntityId), nameof(RoleEntityId), IsUnique = true)]
public class PlayerAccountRoleEntity : TurboEntity
{
    [Column("account_id")]
    public required int PlayerAccountEntityId { get; set; }

    [Column("role_id")]
    public required int RoleEntityId { get; set; }

    [ForeignKey(nameof(PlayerAccountEntityId))]
    public PlayerAccountEntity? PlayerAccount { get; set; }

    [ForeignKey(nameof(RoleEntityId))]
    public RoleEntity? RoleEntity { get; set; }
}
