using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// One rung of the account level ladder shown on the profile. The client renders whatever number
/// the server sends as <c>accountLevel</c> and has no ladder of its own, so the progression is
/// entirely this table's to define — which is why it is a table and not a formula in code.
/// </summary>
[Table("account_levels")]
[Index(nameof(LevelNumber), IsUnique = true)]
public class AccountLevelEntity : VortexEntity
{
    /// <summary>The level reached at <see cref="RequiredScore"/>. Level 1 is the floor.</summary>
    [Column("level_number")]
    public required int LevelNumber { get; set; }

    /// <summary>Achievement score needed to reach this level.</summary>
    [Column("required_score")]
    public required int RequiredScore { get; set; }
}
