using System;
using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.RaidProtection;

namespace Vortex.Rooms.Grains;

/// <summary>
/// One room's live raid-protection state: the owner's settings as loaded, the sliding window of
/// arrivals, and whether an incident or a guard period is running.
/// </summary>
/// <remarks>
/// <para>
/// Settings are loaded once when the room activates and written through on save, so the detector
/// never touches the database on the entry path — which is the path a raid is hammering.
/// </para>
/// <para>
/// <see cref="Arrivals" /> keys on the player, not on the visit. A raider reconnecting forty times
/// is one entry in this window, not forty: counting visits would let a single bored account trip a
/// room's protection against everybody else.
/// </para>
/// </remarks>
internal sealed class RoomRaidProtectionState
{
    /// <summary>How far back an arrival still counts.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Arrivals per sensitivity, within <see cref="Window" />, above which the room is being raided.
    /// </summary>
    /// <remarks>
    /// Ours, not Habbo's — the official server never puts a threshold on the wire, so no client
    /// build can be read for one. These are only the fallbacks; an operator sets the real numbers
    /// under <c>room.raid_protection.threshold.*</c> in the dashboard's config editor, because the
    /// right value depends on how busy the hotel is and nobody can pick it from here.
    /// <para>
    /// Read once when the room loads its settings, not per arrival. A hotel that retunes a
    /// threshold sees it take hold as rooms cycle, which is the same deal every other room-scoped
    /// config key here gets — and the alternative is a grain call on the entry path during a raid.
    /// </para>
    /// </remarks>
    public int ThresholdFor(RaidDetectionSensitivity sensitivity) =>
        sensitivity switch
        {
            RaidDetectionSensitivity.High => HighThreshold,
            RaidDetectionSensitivity.Medium => MediumThreshold,
            _ => LowThreshold,
        };

    public int LowThreshold { get; set; } = 12;

    public int MediumThreshold { get; set; } = 8;

    public int HighThreshold { get; set; } = 5;

    /// <summary>Last arrival per player inside the window. Swept on every evaluation.</summary>
    public Dictionary<PlayerId, DateTime> Arrivals { get; } = [];

    public bool Enabled { get; set; }

    public RaidDetectionSensitivity DetectionSensitivity { get; set; } =
        RaidDetectionSensitivity.Medium;

    public RaidProtectionAction ActionType { get; set; } = RaidProtectionAction.Kick;

    public int BanDurationSeconds { get; set; } = RaidProtectionLimits.DefaultBanDurationSeconds;

    public bool GuardEnabled { get; set; }

    public int GuardDurationSeconds { get; set; } =
        RaidProtectionLimits.DefaultGuardDurationSeconds;

    public RaidDetectionSensitivity GuardSensitivity { get; set; } = RaidDetectionSensitivity.High;

    /// <summary>A raid is being handled right now. Live only — never persisted.</summary>
    public bool IncidentActive { get; set; }

    /// <summary>When the post-incident guard period ends, or null when none is running.</summary>
    public DateTime? GuardUntilUtc { get; set; }

    public DateTime? LastRaidAtUtc { get; set; }

    /// <summary>Whether the settings row has been read from the database yet.</summary>
    public bool Loaded { get; set; }

    /// <summary>Rate limit for saves, so the panel cannot be used to hammer the database.</summary>
    public DateTime LastSaveAtUtc { get; set; } = DateTime.MinValue;

    /// <summary>The threshold in force right now — the guard's while it runs, otherwise detection's.</summary>
    public int CurrentThreshold(DateTime nowUtc) =>
        ThresholdFor(
            GuardUntilUtc is DateTime until && until > nowUtc
                ? GuardSensitivity
                : DetectionSensitivity
        );

    /// <summary>Drops arrivals that have aged out, and answers how many are left.</summary>
    public int CountRecentArrivals(DateTime nowUtc)
    {
        DateTime cutoff = nowUtc - Window;

        // One pass, and the list is small: a room only ever holds as many keys as it saw arrivals in
        // the last minute. Materialising the expired keys first avoids mutating while enumerating.
        List<PlayerId>? expired = null;

        foreach ((PlayerId playerId, DateTime at) in Arrivals)
        {
            if (at < cutoff)
            {
                (expired ??= []).Add(playerId);
            }
        }

        if (expired is not null)
        {
            foreach (PlayerId playerId in expired)
            {
                Arrivals.Remove(playerId);
            }
        }

        return Arrivals.Count;
    }
}
