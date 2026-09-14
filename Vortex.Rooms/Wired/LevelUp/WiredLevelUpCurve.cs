using System;

namespace Vortex.Rooms.Wired.LevelUp;

/// <summary>
/// A level ↔ XP curve: how much experience each level costs, and what a raw amount of experience
/// means in levels.
/// </summary>
/// <remarks>
/// A curve supplies two things — where it stops, and the total XP at which a given level begins.
/// Everything else is derived here, once, because every derived quantity is a statement about
/// <see cref="XpForLevel"/> and re-deriving them per curve is how three curves come to disagree.
/// <para>
/// The client is the reference for the shape but not for the arithmetic, and that is worth saying
/// because it looks like an omission there. AIR's <c>AbstractLevelUpConfig</c> subclasses implement
/// <c>currentLevel</c>, <c>progress</c>, <c>xpRemaining</c> and the rest in <em>one</em> of the
/// three — the linear one; the exponential and manual curves inherit a base that throws from every
/// one of them. They are not incomplete: the client only ever previews a curve, calling
/// <c>xpForLevel</c> and <c>maxLevel</c>, and the levels themselves are the server's answer. That is
/// this class.
/// </para>
/// <para>
/// The generic derivation reproduces the client's linear formulas exactly, which is the check that
/// it is the right generalisation and not merely a plausible one: with <c>XpForLevel(n) =
/// step × (n − 1)</c>, <see cref="CurrentLevel"/> collapses to <c>1 + ⌊xp / step⌋</c>,
/// <see cref="Progress"/> to <c>xp mod step</c>, and <see cref="MaxXp"/> to <c>maxLevel × step</c>.
/// </para>
/// </remarks>
public abstract class WiredLevelUpCurve
{
    /// <summary>The last level this curve can reach. Levels are 1-based.</summary>
    public abstract int MaxLevel { get; }

    /// <summary>Total XP at which <paramref name="level"/> begins; 0 for level 1 and below.</summary>
    public abstract int XpForLevel(int level);

    /// <summary>
    /// The XP at which the level after the last would have begun — the cap every other member
    /// clamps to.
    /// </summary>
    /// <remarks>
    /// One level past the end rather than at it, so that the whole of the final level is spendable.
    /// This is what makes the linear case come out at <c>maxLevel × step</c>, the client's own
    /// value, rather than one step short of it.
    /// </remarks>
    public virtual int MaxXp => XpForLevel(MaxLevel + 1);

    /// <summary>Clamps raw experience into 0..<see cref="MaxXp"/>.</summary>
    public int BoundedValue(int xp) => xp < 0 ? 0 : Math.Min(xp, MaxXp);

    /// <summary>The level <paramref name="xp"/> has reached.</summary>
    /// <remarks>
    /// A binary search rather than a formula, because only the linear curve has one. It needs
    /// <see cref="XpForLevel"/> to be non-decreasing, which the three curves guarantee: linear and
    /// exponential are monotone by construction, and the manual curve rejects any anchor that does
    /// not increase both columns.
    /// </remarks>
    public int CurrentLevel(int xp)
    {
        int bounded = BoundedValue(xp);
        int low = 1;
        int high = MaxLevel;

        while (low < high)
        {
            int mid = low + ((high - low + 1) / 2);

            if (XpForLevel(mid) <= bounded)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    /// <summary>What the current level costs in full; 0 once maxed, since there is nothing to buy.</summary>
    public int TotalXpRequired(int xp)
    {
        if (IsMaxed(xp))
        {
            return 0;
        }

        int level = CurrentLevel(xp);

        return XpForLevel(level + 1) - XpForLevel(level);
    }

    /// <summary>How far into the current level <paramref name="xp"/> stands, in XP.</summary>
    public int Progress(int xp)
    {
        if (IsMaxed(xp))
        {
            return 0;
        }

        return BoundedValue(xp) - XpForLevel(CurrentLevel(xp));
    }

    /// <summary>The same, as a whole percentage, truncated as the client truncates it.</summary>
    public int ProgressPercentage(int xp)
    {
        int required = TotalXpRequired(xp);

        return required <= 0 ? 0 : Progress(xp) * 100 / required;
    }

    /// <summary>XP still owed before the next level.</summary>
    public int XpRemaining(int xp) => IsMaxed(xp) ? 0 : TotalXpRequired(xp) - Progress(xp);

    /// <summary>
    /// Asked of the LEVEL, not of the XP, and the two disagree at the top of the curve.
    /// </summary>
    /// <remarks>
    /// With a step of 100 and 25 levels, level 25 is reached at 2,400 XP while <see cref="MaxXp"/>
    /// is 2,500. Comparing the XP would leave a whole level's worth of "not yet maxed" after the
    /// last level was already reached. The client's linear curve makes the same choice.
    /// </remarks>
    public bool IsMaxed(int xp) => CurrentLevel(xp) >= MaxLevel;
}
