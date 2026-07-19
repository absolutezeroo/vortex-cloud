using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>Furniture-count limit for a Builders Club subscription level
/// (PlayerSubscriptionEntity.Level, SubscriptionType.BuildersClub). Admin-manageable, seeded with
/// sensible defaults -- same idempotent-additive-only convention as SanctionPresetEntity.</summary>
[Table("builders_club_tiers")]
[Index(nameof(Level), IsUnique = true)]
public class BuildersClubTierEntity : VortexEntity
{
    [Column("level")]
    public required int Level { get; set; }

    [Column("furni_limit")]
    public required int FurniLimit { get; set; }
}
