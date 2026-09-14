namespace Vortex.Rooms.Wired.LevelUp;

/// <summary>Every level costs the same.</summary>
/// <remarks>
/// AIR: <c>roomevents/wired_setup/addons/levelupper/LinearLevelUpper.as</c> — the only one of the
/// three whose derived members the client implements, and the case
/// <see cref="WiredLevelUpCurve"/>'s generic derivation is checked against.
/// </remarks>
public sealed class WiredLinearLevelUpCurve(int stepSize, int maxLevel) : WiredLevelUpCurve
{
    private readonly int _stepSize = stepSize < 1 ? 1 : stepSize;

    public override int MaxLevel { get; } = maxLevel < 2 ? 2 : maxLevel;

    public override int XpForLevel(int level) => level <= 1 ? 0 : _stepSize * (level - 1);
}
