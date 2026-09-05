using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Rewards;

/// <summary>
/// Hands over one kind of reward.
/// </summary>
/// <remarks>
/// <para>
/// One implementation per <see cref="RewardKind"/>, resolved from DI by the pipeline. Adding a kind
/// is a new class and an enum member; it is not an edit to a method every other kind shares, which
/// is the whole reason this is not a switch. It also means a granter can be tested against its own
/// service without a reward track anywhere in sight.
/// </para>
/// <para>
/// A granter never opens a transaction or writes a claim. The pipeline owns the operation and the
/// receipts; a granter is handed an operation id and a step key and does one thing with them.
/// </para>
/// </remarks>
internal interface IRewardGranter
{
    /// <summary>The kind this granter handles. Two granters claiming the same kind is a wiring bug.</summary>
    RewardKind Kind { get; }

    /// <summary>
    /// Hands the reward to the player.
    /// </summary>
    /// <param name="stepKey">
    /// The receipt this grant is recorded under. Unique within the operation, so a retried claim
    /// re-runs the steps that did not land and skips the ones that did.
    /// </param>
    /// <returns>
    /// False when the reward could not be handed over for a reason that is the content's fault — a
    /// furniture id that does not exist, a currency with no row. Throwing is for a failure that
    /// should be retried; returning false is for one that never will succeed.
    /// </returns>
    Task<bool> GrantAsync(
        int playerId,
        RewardGrantSnapshot reward,
        CommerceOperationId operation,
        string stepKey,
        CancellationToken ct
    );
}
