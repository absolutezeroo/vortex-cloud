using Vortex.Primitives.Rooms.Games;
using Vortex.Rooms.Games.Presentation;

namespace Vortex.Rooms.Games.Freeze;

/// <summary>
/// The wire-fixed constants of the Freeze game — the avatar effect ids and the client-matched snowball
/// animation timing. These are protocol values, not balance, so they stay compiled in; every tunable
/// balance value (lives, ammo, timers, chance, points) is admin-editable server config, see
/// <see cref="FreezeConfig"/> / <see cref="FreezeSettings"/>.
/// <para>
/// Effect ids: a joined player wears <c>TeamEffectBase + team</c> (team 1-4), a frozen player wears
/// <see cref="FrozenEffect"/>, and a shielded player adds <see cref="ProtectionEffectBonus"/>. Verified
/// against the client's effectmap.json: 12 = Ice, 40-43 = ESred/green/blue/yellow (team), 49-52 =
/// ES*Untouchable (shielded, i.e. team + 9).
/// </para>
/// </summary>
public static class FreezeConstants
{
    /// <summary>The game's identity. Every Freeze component carries it and the runtime routes on it.</summary>
    public static readonly GameId Game = new("freeze");

    public const int TeamEffectBase = (int)GameAuraSet.Freeze;

    public const int FrozenEffect = 12;

    public const int ProtectionEffectBonus = 9;

    public const int NoEffect = 0;

    public const int FreezeTickMs = 1000;

    public const int BlastDelayMs = 2000;

    public const int ResetDelayMs = 3000;

    public const int StateWireScale = 1000;

    public const int TileIdle = 0;

    public const int TileBlast = 11;

    public const int BlockIntact = 0;

    public const int BlockEmpty = 1;

    public const int BlockCollectedOffset = 10;
}
