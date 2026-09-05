using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Progression;

/// <summary>
/// The premium point multiplier.
/// </summary>
/// <remarks>
/// <para>
/// Integer arithmetic on purpose. A multiplier stored as a double and applied with
/// <c>(int)(points * 1.2)</c> gives 29 for 25 points about as often as it gives 30, depending on
/// which decimal the operator typed and how it rounded on the way in — and a progression system
/// that pays a different number for the same work on two servers is the kind of bug nobody
/// reproduces. Per-mille integers and one explicit rounding rule make the answer the same
/// everywhere, forever.
/// </para>
/// <para>
/// Rounding is half-up, away from zero. 25 at 1.2× is 30 exactly; 25 at 1.15× is 28.75, and the
/// player gets 29. Rounding down would quietly shave a point off most grants, which reads as the
/// boost not working.
/// </para>
/// </remarks>
internal static class PremiumBoost
{
    /// <summary>
    /// The points to actually grant.
    /// </summary>
    /// <param name="premiumActive">
    /// Whether premium was active <em>at the moment the stage was reached</em>. Points earned
    /// before a purchase are never revisited: the boost buys faster progress from here, not a
    /// retroactive top-up of everything already banked.
    /// </param>
    public static int Apply(int basePoints, RewardTrackPremiumSnapshot? premium, bool premiumActive)
    {
        if (basePoints <= 0 || premium is null || !premiumActive)
        {
            return basePoints;
        }

        int perMille = premium.BoostPerMille;

        // A boost below 1.0x is content nobody meant to write; treating it as no boost beats
        // charging a player for slower progress.
        if (perMille <= 1000)
        {
            return basePoints;
        }

        return (int)(((long)basePoints * perMille + 500) / 1000);
    }
}
