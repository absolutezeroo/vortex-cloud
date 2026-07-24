namespace Vortex.Rooms.Grains.Systems.Freeze;

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
    // --- avatar effects (wire-fixed) ---
    public const int TeamEffectBase = 39; // Red(1)->40, Green(2)->41, Blue(3)->42, Yellow(4)->43
    public const int FrozenEffect = 12;
    public const int ProtectionEffectBonus = 9;
    public const int NoEffect = 0;

    // --- game tick + snowball animation timing, in ms (rise now -> blast -> reset) ---
    public const int FreezeTickMs = 1000;
    public const int BlastDelayMs = 2000;
    public const int ResetDelayMs = 3000;

    // --- furni animation states (wire-fixed) ---
    // All Freeze furni use the client's IceStorm logic, which decodes a sent value as
    // actualState = wire / StateWireScale and a deferred-transition delay = wire % StateWireScale.
    // So a state is put on the wire as `state * StateWireScale`, verified against the es_tile / es_box
    // furni assets (visualization.xml).
    public const int StateWireScale = 1000;

    // es_tile (freeze_tile): idle=0 (throwable), rise=(blastRadius + 1) in 1..MaxBoost+1, blast=11.
    public const int TileIdle = 0;
    public const int TileBlast = 11;

    // es_box (freeze_block): 0=intact (solid), 1=destroyed-empty, 2..7=destroyed showing a power-up icon
    // (see FreezePowerUps.RevealState), and a collected block is its reveal state + 10 (12..17), which
    // makes the client play the icon-fade transition.
    public const int BlockIntact = 0;
    public const int BlockEmpty = 1;
    public const int BlockCollectedOffset = 10;
}
