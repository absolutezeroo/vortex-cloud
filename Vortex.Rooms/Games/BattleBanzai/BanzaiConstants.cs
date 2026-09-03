using Vortex.Primitives.Rooms.Games;

namespace Vortex.Rooms.Games.BattleBanzai;

/// <summary>
/// The wire-fixed constants of Battle Banzai. The client has NO banzai logic type — a
/// <c>bb_patch1</c> is a plain multistate furni whose visualization maps these state numbers to
/// colours, so the encoding below IS the wire contract (verified against Arcturus
/// <c>InteractionBattleBanzaiTile</c> / <c>BattleBanzaiGame</c>, whose numbers the asset states
/// match): state 0 = off, 1 = neutral in-round, and team <c>t</c> (Red=1..Yellow=4) claims through
/// <c>t*3</c> → <c>t*3+1</c> → <c>t*3+2</c> (locked). Everything tunable (points, caps) is server
/// config — see <see cref="BanzaiConfig"/>.
/// </summary>
public static class BanzaiConstants
{
    /// <summary>The game's identity. Every Banzai component carries it and the runtime routes on it.</summary>
    public static readonly GameId Game = new("banzai");

    // --- bb_patch1 states (wire-fixed) ---
    public const int TileOff = 0;
    public const int TileNeutral = 1;

    /// <summary>First claim state for a team: <c>TeamStateBase * (int)team</c> (Red 3 … Yellow 12).</summary>
    public const int TeamStateBase = 3;

    /// <summary>Offset within a team's three states that means "locked" (t*3 + 2).</summary>
    public const int LockedOffset = 2;

    // --- round end ---
    /// <summary>The winning team's locked tiles blink between 0 and their locked state.</summary>
    public const int FlickerCount = 10;
    public const int FlickerIntervalMs = 500;

    /// <summary>How long the runtime holds the round-ending phase: exactly the celebration.</summary>
    public const int RoundEndMs = FlickerCount * FlickerIntervalMs;

    // --- bb_rnd_tele ---
    public const int TeleportDelayMs = 500;
    public const int TeleportActiveState = 1;
    public const int TeleportIdleState = 0;

    /// <summary>A teleporter landing on another teleporter chains; this caps the chain so two
    /// teleporters can never bounce an avatar forever.</summary>
    public const int TeleportChainCap = 4;
}
