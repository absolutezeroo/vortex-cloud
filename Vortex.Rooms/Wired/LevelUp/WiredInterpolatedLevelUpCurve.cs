using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vortex.Rooms.Wired.LevelUp;

/// <summary>
/// A hand-written curve: the builder names some levels and what they cost, and the levels in
/// between are interpolated.
/// </summary>
/// <remarks>
/// AIR: <c>roomevents/wired_setup/addons/levelupper/InterpolateLevelUpper.as</c>, fed by the
/// add-on's string param — one <c>level=xp</c> per line.
/// <para>
/// Both columns must strictly increase, line by line, and a single line that does not voids the
/// whole curve rather than being skipped. That is the client's rule
/// (<c>VariableLevelUp.parseLevelToXpMap</c> returns null and the preview goes blank), and it has to
/// be the same one here: a curve that silently dropped a bad anchor would level players on a shape
/// nobody previewed.
/// </para>
/// </remarks>
public sealed class WiredInterpolatedLevelUpCurve : WiredLevelUpCurve
{
    /// <summary>XP → level, ascending. Seeded with 0 → 1, as the client seeds its tree.</summary>
    private readonly List<(int Xp, int Level)> _anchors;

    private WiredInterpolatedLevelUpCurve(List<(int Xp, int Level)> anchors)
    {
        _anchors = anchors;
        MaxLevel = anchors[^1].Level;
    }

    public override int MaxLevel { get; }

    /// <summary>
    /// Parses the add-on's string param, or returns null if any line breaks the increase rule.
    /// </summary>
    public static WiredInterpolatedLevelUpCurve? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        List<(int Xp, int Level)> anchors = [(0, 1)];
        int lastLevel = 1;
        int lastXp = 0;

        foreach (string line in text.Split('\n'))
        {
            string[] parts = line.Split('=', 2);

            if (parts.Length < 2)
            {
                continue;
            }

            int level = AsInt(parts[0]);
            int xp = AsInt(parts[1]);

            if (level <= lastLevel || xp <= lastXp)
            {
                return null;
            }

            anchors.Add((xp, level));
            lastLevel = level;
            lastXp = xp;
        }

        return anchors.Count > 1 ? new WiredInterpolatedLevelUpCurve(anchors) : null;
    }

    public override int XpForLevel(int level)
    {
        if (level <= 1)
        {
            return 0;
        }

        for (int i = 0; i < _anchors.Count; i++)
        {
            (int xp, int anchorLevel) = _anchors[i];

            if (anchorLevel == level)
            {
                return xp;
            }

            if (anchorLevel > level)
            {
                (int lowerXp, int lowerLevel) = _anchors[i - 1];
                double xpPerLevel = (double)(xp - lowerXp) / (anchorLevel - lowerLevel);

                return lowerXp + (int)(xpPerLevel * (level - lowerLevel));
            }
        }

        // Past the last anchor: the curve stops there, so the last anchor's XP is its ceiling. This
        // is also what makes MaxXp sit exactly at the top level rather than one level beyond it.
        return _anchors[^1].Xp;
    }

    /// <summary>AS3's <c>int()</c> never throws — anything unparseable is zero, and zero fails the
    /// increase rule on the next line, which is the behaviour to keep.</summary>
    private static int AsInt(string value) =>
        int.TryParse(
            value.Trim(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsed
        )
            ? parsed
            : 0;
}
