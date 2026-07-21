using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Quests;

/// <summary>
/// Definition of a single quest. Quests are grouped into campaigns (<see cref="CampaignCode"/>) and
/// chains (<see cref="ChainCode"/>); the client localizes them via
/// <c>"quests." + CampaignCode + "." + LocalizationCode</c>. <see cref="QuestType"/> names the
/// objective (which gameplay event advances it), and <see cref="TotalSteps"/> is the goal.
/// </summary>
[Table("quests")]
[Index(nameof(CampaignCode))]
public class QuestEntity : VortexEntity
{
    [Column("campaign_code")]
    public required string CampaignCode { get; set; }

    [Column("chain_code")]
    [DefaultValue("")]
    public string ChainCode { get; set; } = string.Empty;

    [Column("localization_code")]
    public required string LocalizationCode { get; set; }

    /// <summary>The objective type; matches a progression trigger (e.g. "RoomEntry", "CatalogPurchase").</summary>
    [Column("quest_type")]
    public required string QuestType { get; set; }

    /// <summary>
    /// Optional parameterised target kind for the objective (e.g. "offer_id", "base_item_id"), à la
    /// Arcturus. Empty = the objective advances on any occurrence; set = only when a trigger fires
    /// with a matching <see cref="TargetValue"/> (e.g. "buy THIS offer", "switch THIS furni type").
    /// </summary>
    [Column("target_type")]
    [DefaultValue("")]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>The specific target value paired with <see cref="TargetType"/> (e.g. an offer id).</summary>
    [Column("target_value")]
    [DefaultValue("")]
    public string TargetValue { get; set; } = string.Empty;

    /// <summary>When false the quest is hidden from players and never progresses (disable without
    /// deleting — the admin can still see it).</summary>
    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

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
