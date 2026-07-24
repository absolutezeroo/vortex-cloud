using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>
/// The kind of power-up an ice block can drop when it is destroyed. Each maps to one loadout change on
/// the collecting player's <see cref="FreezePlayerState"/>. These are the six classic Habbo Freeze
/// pick-ups; the integer value is the game-logic id only — the furni <em>state</em> the client renders
/// the icon from is mapped separately (wire-truth), so this order is free to change without touching the
/// sprite the player sees.
/// </summary>
public enum FreezePowerUp
{
    None = 0,

    /// <summary>+1 carried snowball (ammo), up to the max.</summary>
    ExtraSnowball = 1,

    /// <summary>+1 permanent blast radius, up to the max.</summary>
    LongerRange = 2,

    /// <summary>+1 life, up to the max.</summary>
    ExtraLife = 3,

    /// <summary>A temporary shield: immune to being frozen until it expires.</summary>
    Shield = 4,

    /// <summary>The next throw also hits the four diagonals (X-blast).</summary>
    XBlast = 5,

    /// <summary>The next throw fires at the maximum blast radius (Mega Snowball).</summary>
    Mega = 6,
}

/// <summary>
/// Pure rules for Freeze power-ups: the pickable set and how each one changes a player. IO-free and
/// unit-tested; <see cref="RoomFreezeSystem"/> rolls a drop when a block breaks, renders it and applies
/// it on walk-on.
/// </summary>
public static class FreezePowerUps
{
    /// <summary>The power-ups a destroyed block can drop, in game-logic id order.</summary>
    public static readonly FreezePowerUp[] Pickable =
    [
        FreezePowerUp.ExtraSnowball,
        FreezePowerUp.LongerRange,
        FreezePowerUp.ExtraLife,
        FreezePowerUp.Shield,
        FreezePowerUp.XBlast,
        FreezePowerUp.Mega,
    ];

    /// <summary>The power-up dropped for a roll in <c>[0, Pickable.Length)</c>. Split out from the runtime
    /// RNG so the choice is deterministic under test.</summary>
    public static FreezePowerUp Pick(int roll) => Pickable[roll % Pickable.Length];

    /// <summary>The <c>es_box</c> furni state (2..7) that shows this power-up's icon. Verified against the
    /// real es_box furni asset's icon frames — the icon the client draws for each state:
    /// 2 = X-blast (diagonal arrows), 3 = snowballs (blue cluster), 4 = longer range (green cross),
    /// 5 = mega (orange ball), 6 = extra life (heart), 7 = shield.</summary>
    public static int RevealState(FreezePowerUp powerUp) =>
        powerUp switch
        {
            FreezePowerUp.XBlast => 2,
            FreezePowerUp.ExtraSnowball => 3,
            FreezePowerUp.LongerRange => 4,
            FreezePowerUp.Mega => 5,
            FreezePowerUp.ExtraLife => 6,
            FreezePowerUp.Shield => 7,
            _ => FreezeConstants.BlockEmpty,
        };

    /// <summary>The power-up shown by an <c>es_box</c> reveal state (2..7), or <c>None</c> for any other
    /// state (intact/empty/collected).</summary>
    public static FreezePowerUp FromRevealState(int state) =>
        state switch
        {
            2 => FreezePowerUp.XBlast,
            3 => FreezePowerUp.ExtraSnowball,
            4 => FreezePowerUp.LongerRange,
            5 => FreezePowerUp.Mega,
            6 => FreezePowerUp.ExtraLife,
            7 => FreezePowerUp.Shield,
            _ => FreezePowerUp.None,
        };

    /// <summary>Applies <paramref name="powerUp"/> to <paramref name="player"/>'s loadout.</summary>
    public static void Apply(FreezePowerUp powerUp, FreezePlayerState player)
    {
        switch (powerUp)
        {
            case FreezePowerUp.ExtraSnowball:
                player.AddSnowball();

                break;
            case FreezePowerUp.LongerRange:
                player.IncreaseBoost();

                break;
            case FreezePowerUp.ExtraLife:
                player.AddLife();

                break;
            case FreezePowerUp.Shield:
                player.AddProtection();

                break;
            case FreezePowerUp.XBlast:
                player.NextDiagonal = true;

                break;
            case FreezePowerUp.Mega:
                player.TempMassive = true;

                break;
        }
    }
}
