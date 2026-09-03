using Vortex.Rooms.Games.Scoring;

namespace Vortex.Rooms.Games.Freeze;

/// <summary>Why Freeze awarded (or deducted) points. Carried on every score it makes.</summary>
public static class FreezeScoreReasons
{
    public static readonly ScoreReason PlayerFrozen = new("freeze.player_frozen");

    /// <summary>Catching your own team, or yourself, in a blast. The same magnitude, negated.</summary>
    public static readonly ScoreReason FriendlyFire = new("freeze.friendly_fire");

    public static readonly ScoreReason BlockDestroyed = new("freeze.block_destroyed");

    public static readonly ScoreReason PowerUpCollected = new("freeze.powerup_collected");
}
