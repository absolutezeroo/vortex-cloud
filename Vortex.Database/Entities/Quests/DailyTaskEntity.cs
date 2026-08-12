using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Quests;

/// <summary>
/// A daily-task definition. Assignments (<see cref="PlayerDailyTaskEntity"/>) are drawn from these:
/// the ordinary ones make up a player's board for the day, and a bonus is added on top.
/// </summary>
[Table("daily_tasks")]
[Index(nameof(TaskCode), IsUnique = true)]
public class DailyTaskEntity : VortexEntity
{
    /// <summary>Localization stem the client renders name, description and hint from.</summary>
    [Column("task_code")]
    public required string TaskCode { get; set; }

    /// <summary>The objective that advances it, from the same vocabulary quests use.</summary>
    [Column("quest_type_code")]
    public required string QuestTypeCode { get; set; }

    /// <summary>Bonus tasks are drawn separately and sort after the ordinary ones.</summary>
    [Column("is_bonus")]
    [DefaultValue(false)]
    public bool IsBonus { get; set; }

    [Column("image_version")]
    [DefaultValue("")]
    public string ImageVersion { get; set; } = string.Empty;

    /// <summary>Catalog page the task's button opens; empty for no button.</summary>
    [Column("catalog_name")]
    [DefaultValue("")]
    public string CatalogName { get; set; } = string.Empty;

    /// <summary>How many times the objective must fire.</summary>
    [Column("required_repeats")]
    [DefaultValue(1)]
    public int RequiredRepeats { get; set; } = 1;

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }
}
