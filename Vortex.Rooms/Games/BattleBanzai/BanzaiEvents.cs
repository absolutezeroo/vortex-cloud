using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;

namespace Vortex.Rooms.Games.BattleBanzai;

/// <summary>Why Battle Banzai awarded points. Carried on every score it makes, so a match's tally is
/// reconstructable from the log and an achievement or quest later has something to key on.</summary>
public static class BanzaiScoreReasons
{
    /// <summary>Advancing your own tile's claim (first or second step).</summary>
    public static readonly ScoreReason TileFilled = new("banzai.tile_filled");

    /// <summary>Stealing a neutral or enemy unlocked tile.</summary>
    public static readonly ScoreReason TileHijacked = new("banzai.tile_hijacked");

    /// <summary>Completing a tile's third step, plus every tile of any region it enclosed.</summary>
    public static readonly ScoreReason TileLocked = new("banzai.tile_locked");
}

/// <summary>
/// A team enclosed and locked a region. The one Banzai-specific event worth raising: it is the play
/// that decides a match, it is invisible in the score alone (a big region and a long grind look the
/// same), and it is what a wired trigger would want to react to.
/// </summary>
public sealed record BanzaiRegionLockedEvent : GameEvent
{
    public required TeamId Team { get; init; }

    public required int TileCount { get; init; }
}
