using System;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>
/// The per-player state of one Freeze participant: their team, lives, ammo, the boosts a picked-up
/// power-up granted, and the frozen/protected timers counted down each game tick. Pure state + rules,
/// no IO — <see cref="RoomFreezeSystem"/> turns the changes here into effect broadcasts, teleports and
/// score. Balance comes from an injected <see cref="FreezeSettings"/> (live server config); only the
/// effect ids are compiled constants. Mutators return whether a visible change happened.
/// </summary>
public sealed class FreezePlayerState(
    PlayerId playerId,
    GameTeamColor team,
    FreezeSettings settings
)
{
    private readonly FreezeSettings _settings = settings;

    public PlayerId PlayerId { get; } = playerId;
    public GameTeamColor Team { get; } = team;

    public int Lives { get; private set; } = settings.StartLives;
    public int Snowballs { get; private set; } = settings.StartSnowballs;
    public int ExplosionBoost { get; private set; }
    public int FrozenTicks { get; private set; }
    public int ProtectionTicks { get; private set; }

    private int _regenTicks;

    /// <summary>The next throw also hits the four diagonals (X-blast). Consumed after one throw.</summary>
    public bool NextDiagonal { get; set; }

    /// <summary>The next throw hits the cardinal arms. Persists (default throw shape).</summary>
    public bool NextHorizontal { get; private set; } = true;

    /// <summary>The next throw forces the maximum blast radius exactly once (Mega Snowball).</summary>
    public bool TempMassive { get; set; }

    public bool Dead { get; private set; }

    public bool IsFrozen => FrozenTicks > 0;
    public bool IsProtected => ProtectionTicks > 0;

    /// <summary>Can be frozen only when neither already frozen nor shielded.</summary>
    public bool CanBeFrozen => !IsFrozen && !IsProtected;

    /// <summary>Has a snowball to throw and is able to act.</summary>
    public bool CanThrow => Snowballs > 0 && !IsFrozen && !Dead;

    /// <summary>Reset to the starting loadout for a fresh game.</summary>
    public void Reset()
    {
        Lives = _settings.StartLives;
        Snowballs = _settings.StartSnowballs;
        ExplosionBoost = 0;
        FrozenTicks = 0;
        ProtectionTicks = 0;
        NextDiagonal = false;
        NextHorizontal = true;
        TempMassive = false;
        Dead = false;
        _regenTicks = 0;
    }

    /// <summary>The blast radius (arm length) of the next throw; Mega forces the max once.</summary>
    public int TakeThrowRadius()
    {
        if (TempMassive)
        {
            TempMassive = false;

            return _settings.MaxExplosionBoost;
        }

        return ExplosionBoost;
    }

    public void SpendSnowball() => Snowballs = Math.Max(0, Snowballs - 1);

    public void AddSnowball(int amount = 1) =>
        Snowballs = Math.Clamp(Snowballs + amount, 0, _settings.MaxSnowballs);

    public void IncreaseBoost() =>
        ExplosionBoost = Math.Clamp(ExplosionBoost + 1, 0, _settings.MaxExplosionBoost);

    public void AddLife() => Lives = Math.Min(_settings.MaxLives, Lives + 1);

    public bool CanPickUpLife => Lives < _settings.StartLives;

    public void AddProtection()
    {
        ProtectionTicks = _settings.ProtectionStacks
            ? ProtectionTicks + _settings.ProtectionTicks
            : _settings.ProtectionTicks;
    }

    /// <summary>Applies a freeze hit: costs a life, freezes for the frozen duration and drops some ammo
    /// and boost. Returns <c>true</c> if the player just died (lives hit zero).</summary>
    public bool Freeze()
    {
        if (!CanBeFrozen)
        {
            return false;
        }

        Lives--;
        FrozenTicks = _settings.FrozenTicks;
        Snowballs = Math.Max(0, Snowballs - _settings.FreezeLoseSnowballs);
        ExplosionBoost = Math.Max(0, ExplosionBoost - _settings.FreezeLoseBoost);

        if (Lives <= 0)
        {
            Dead = true;

            return true;
        }

        return false;
    }

    /// <summary>One game tick: counts the frozen/protection timers down and slowly regenerates ammo.
    /// Returns <c>true</c> when the player just thawed (frozen reached zero this tick), so the wrapper
    /// refreshes their effect.</summary>
    public bool Tick()
    {
        bool thawed = false;

        if (ProtectionTicks > 0)
        {
            ProtectionTicks--;
        }

        if (FrozenTicks > 0)
        {
            FrozenTicks--;
            thawed = FrozenTicks == 0;
        }

        RegenerateSnowball();

        return thawed;
    }

    /// <summary>Refills one snowball every <c>SnowballRegenTicks</c> ticks while able to act and below the
    /// max, so a player is never left permanently out of ammo. Paused (and the counter reset) while
    /// frozen, dead, full or when regen is disabled.</summary>
    private void RegenerateSnowball()
    {
        if (
            _settings.SnowballRegenTicks <= 0
            || IsFrozen
            || Dead
            || Snowballs >= _settings.MaxSnowballs
        )
        {
            _regenTicks = 0;

            return;
        }

        if (++_regenTicks >= _settings.SnowballRegenTicks)
        {
            _regenTicks = 0;
            AddSnowball();
        }
    }

    /// <summary>The avatar effect id this player should currently show.</summary>
    public int CurrentEffect()
    {
        if (Dead)
        {
            return FreezeConstants.NoEffect;
        }

        if (IsFrozen)
        {
            return FreezeConstants.FrozenEffect;
        }

        int teamEffect = FreezeConstants.TeamEffectBase + (int)Team;

        return IsProtected ? teamEffect + FreezeConstants.ProtectionEffectBonus : teamEffect;
    }
}
