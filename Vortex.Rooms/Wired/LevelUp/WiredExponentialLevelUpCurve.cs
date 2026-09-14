using System;

namespace Vortex.Rooms.Wired.LevelUp;

/// <summary>Each level costs a fixed percentage more than the one before.</summary>
/// <remarks>
/// AIR: <c>roomevents/wired_setup/addons/levelupper/ExponentialLevelUpper.as</c>. The increase
/// factor arrives as a whole percentage — the form's label is literally "Increase factor (%)" — and
/// is divided by 100 here, exactly where the client divides it.
/// <para>
/// The <c>1e-9</c> is the client's and is kept: <c>Math.Pow</c> lands a hair under an integer for
/// factors like 20%, and truncating that is a level boundary one XP too late.
/// </para>
/// </remarks>
public sealed class WiredExponentialLevelUpCurve : WiredLevelUpCurve
{
    private readonly double _firstLevelXp;

    private readonly double _strength;

    public WiredExponentialLevelUpCurve(int firstLevelXp, int increaseFactor, int maxLevel)
    {
        _firstLevelXp = firstLevelXp < 1 ? 1 : firstLevelXp;
        _strength = (increaseFactor < 1 ? 1 : increaseFactor) / 100d;
        MaxLevel = maxLevel < 2 ? 2 : maxLevel;
    }

    public override int MaxLevel { get; }

    public override int XpForLevel(int level)
    {
        if (level < 1)
        {
            return 0;
        }

        double xp = _firstLevelXp * ((Math.Pow(1 + _strength, level - 1) - 1 + 1e-9) / _strength);

        return xp >= int.MaxValue ? int.MaxValue : (int)xp;
    }
}
