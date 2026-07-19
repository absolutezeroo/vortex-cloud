using System;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Players.Grains;

public sealed class PlayerLiveState
{
    public required PlayerId PlayerId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Motto { get; set; } = string.Empty;
    public string Figure { get; set; } = string.Empty;
    public AvatarGenderType Gender { get; set; } = AvatarGenderType.Male;
    public int AchievementScore { get; set; } = 0;
    public int RespectReceived { get; set; } = 0;
    public int RespectGivenToday { get; set; } = 0;
    public DateTime? RespectResetDate { get; set; } = null;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public int ClubLevel { get; set; } = 0;
    public DateTime ClubExpiresAt { get; set; } = DateTime.MinValue;
    public int ClubTotalMonths { get; set; } = 0;
    public int ClubGiftsAvailable { get; set; } = 0;
    public DateTime? ClubNextGiftAt { get; set; } = null;
    public int ClubPastClubDays { get; set; } = 0;
    public int ClubPastVipDays { get; set; } = 0;
    public DateTime? ClubFirstSubscribedAt { get; set; } = null;
    public DateTime? ClubLastExpiredAt { get; set; } = null;
    public bool ClubBadgeGranted { get; set; } = false;

    public DateTime? KickbackPaydayAt { get; set; } = null;
    public int KickbackCreditsSpent { get; set; } = 0;
    public int KickbackTotalRewarded { get; set; } = 0;
    public int KickbackTotalSpent { get; set; } = 0;
}
