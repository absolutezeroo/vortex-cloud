using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Quests;

/// <summary>
/// Definition of a single quest. Quests are grouped into campaigns (<see cref="CampaignCode"/>) and
/// chains (<see cref="ChainCode"/>); the client localizes them via
/// <c>"quests." + CampaignCode + "." + LocalizationCode</c>. <see cref="QuestType"/> names the
/// objective (which gameplay event advances it), and <see cref="TotalSteps"/> is the goal.
/// </summary>
[Table("quests")]
[Index(nameof(CampaignCode))]
public class QuestEntity : TurboEntity
{
    [Column("campaign_code")]
    public required string CampaignCode { get; set; }

    [Column("chain_code")]
    [DefaultValue("")]
    public string ChainCode { get; set; } = string.Empty;

    [Column("localization_code")]
    public required string LocalizationCode { get; set; }

    /// <summary>The objective type; matches a progression trigger (e.g. "GamePlayed", "RoomEntry").</summary>
    [Column("quest_type")]
    public required string QuestType { get; set; }

    [Column("total_steps")]
    public required int TotalSteps { get; set; }

    [Column("reward_type")]
    public int RewardType { get; set; }

    [Column("reward_amount")]
    public int RewardAmount { get; set; }

    [Column("catalog_page_name")]
    [DefaultValue("")]
    public string CatalogPageName { get; set; } = string.Empty;

    [Column("image_version")]
    [DefaultValue("")]
    public string ImageVersion { get; set; } = string.Empty;

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("easy")]
    [DefaultValue(false)]
    public bool Easy { get; set; }

    [Column("seasonal")]
    [DefaultValue(false)]
    public bool Seasonal { get; set; }

    /// <summary>Lifetime in seconds for a seasonal quest (fallback when <see cref="EndsAt"/> is null).</summary>
    [Column("seasonal_seconds")]
    [DefaultValue(0)]
    public int SeasonalSeconds { get; set; }

    /// <summary>Absolute end time of a seasonal quest; the wire seconds-left is <c>EndsAt - now</c>.</summary>
    [Column("ends_at")]
    public DateTime? EndsAt { get; set; }
}
