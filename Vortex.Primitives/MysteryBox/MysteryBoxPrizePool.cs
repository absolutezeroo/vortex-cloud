namespace Vortex.Primitives.MysteryBox;

/// <summary>Which draw a prize row belongs to.</summary>
public enum MysteryBoxPrizePool
{
    /// <summary>Drawn when a box and a matching key are opened together.</summary>
    Box = 0,

    /// <summary>Drawn when a mystery trophy is inscribed and opened.</summary>
    Trophy = 1,
}
