namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>
/// The wire-fixed constants of the Freeze game — the avatar effect ids and the client-matched snowball
/// animation timing. These are protocol values, not balance, so they stay compiled in; every tunable
/// balance value (lives, ammo, timers, chance, points) is admin-editable server config, see
/// <see cref="FreezeConfig"/> / <see cref="FreezeSettings"/>.
/// <para>
/// Effect ids: a joined player wears <c>TeamEffectBase + team</c> (team 1-4), a frozen player wears
/// <see cref="FrozenEffect"/>, and a shielded player adds <see cref="ProtectionEffectBonus"/>. These
/// mirror the Habbo Freeze effect set (as used by Arcturus); verify against the WIN63 client.
/// </para>
/// </summary>
public static class FreezeConstants
{
    // --- avatar effects (wire-fixed) ---
    public const int TeamEffectBase = 39; // Red(1)->40, Green(2)->41, Blue(3)->42, Yellow(4)->43
    public const int FrozenEffect = 12;
    public const int ProtectionEffectBonus = 9;
    public const int NoEffect = 0;

    // --- game tick + snowball animation timing, in ms (rise now -> blast -> reset) ---
    public const int FreezeTickMs = 1000;
    public const int BlastDelayMs = 2000;
    public const int ResetDelayMs = 3000;
}
