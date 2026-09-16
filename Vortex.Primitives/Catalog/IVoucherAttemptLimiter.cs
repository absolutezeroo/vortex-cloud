using Vortex.Primitives.Players;

namespace Vortex.Primitives.Catalog;

/// <summary>
/// Counts wrong voucher codes per player so guessing costs more than being told no (SEC-11).
/// Successful redemptions are free.
/// </summary>
public interface IVoucherAttemptLimiter
{
    /// <summary>False when this player has spent their allowance of wrong codes.</summary>
    bool MayAttempt(PlayerId playerId);

    void RecordFailure(PlayerId playerId);
}
