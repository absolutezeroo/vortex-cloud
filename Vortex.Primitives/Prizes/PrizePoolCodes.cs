namespace Vortex.Primitives.Prizes;

/// <summary>
/// Pool codes the server draws by name. A pool referenced here is one the code path cannot work
/// without, so it is seeded and must exist; every other pool is pure operator data and is reached
/// through whatever binding its trigger uses.
/// </summary>
public static class PrizePoolCodes
{
    /// <summary>Drawn when a box and a matching key are opened together.</summary>
    public const string MysteryBox = "mystery-box";

    /// <summary>Drawn when a mystery trophy is inscribed and opened.</summary>
    public const string MysteryTrophy = "mystery-trophy";
}
