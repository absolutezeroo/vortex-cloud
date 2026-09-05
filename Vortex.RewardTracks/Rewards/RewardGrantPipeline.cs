using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.RewardTracks.Rewards;

/// <summary>What handing over a whole bundle did.</summary>
/// <param name="Granted">How many rewards landed.</param>
/// <param name="Failed">How many were refused by their granter, or had no granter at all.</param>
/// <param name="Summary">
/// What was handed over, rendered for the claim row: <c>currency:0x100, badge:ACH_Foo</c>. Kept on
/// the claim because the prize definition can be rewritten afterwards and this cannot.
/// </param>
public readonly record struct RewardBundleResult(int Granted, int Failed, string Summary)
{
    public bool Complete => Failed == 0;
}

/// <summary>
/// Hands over a prize's whole bundle, one reward at a time, under one operation.
/// </summary>
/// <remarks>
/// <para>
/// Transactional semantics, stated deliberately rather than left to chance: the claim row is
/// written <em>before</em> this runs, so the bundle is past its pivot. A reward that fails here is
/// therefore <em>owed</em>, not rolled back — the operation stays open at the step that failed and
/// an operator can see which one. Rolling back instead would mean taking a badge off a player
/// because the ducket grant timed out, and re-granting the prize on a retry would hand out the
/// badge twice.
/// </para>
/// <para>
/// Each reward carries its own indexed step key, so a retry re-runs the ones that did not land and
/// skips the ones that did.
/// </para>
/// </remarks>
internal sealed class RewardGrantPipeline
{
    private readonly IReadOnlyDictionary<RewardKind, IRewardGranter> _granters;
    private readonly ICommerceJournal _journal;
    private readonly ILogger<RewardGrantPipeline> _logger;

    public RewardGrantPipeline(
        IEnumerable<IRewardGranter> granters,
        ICommerceJournal journal,
        ILogger<RewardGrantPipeline> logger
    )
    {
        Dictionary<RewardKind, IRewardGranter> byKind = [];

        foreach (IRewardGranter granter in granters)
        {
            // Last registration wins rather than throwing at startup. A duplicate is a wiring
            // mistake worth a loud line in the log, not a hotel that refuses to boot.
            if (!byKind.TryAdd(granter.Kind, granter))
            {
                logger.LogWarning(
                    "Two reward granters claim {Kind}; {Winner} will be used.",
                    granter.Kind,
                    granter.GetType().Name
                );

                byKind[granter.Kind] = granter;
            }
        }

        _granters = byKind;
        _journal = journal;
        _logger = logger;
    }

    public async Task<RewardBundleResult> GrantAsync(
        int playerId,
        string trackId,
        string prizeId,
        ImmutableArray<RewardGrantSnapshot> rewards,
        CommerceOperationId operation,
        CancellationToken ct
    )
    {
        int granted = 0;
        int failed = 0;
        StringBuilder summary = new();

        for (int i = 0; i < rewards.Length; i++)
        {
            RewardGrantSnapshot reward = rewards[i];

            if (!_granters.TryGetValue(reward.Kind, out IRewardGranter? granter))
            {
                // Content naming a kind this build cannot hand over. Reported, and the rest of the
                // bundle still goes out: losing one reward beats losing all of them.
                _logger.LogError(
                    "No granter for reward kind {Kind} on {TrackId}/{PrizeId}; player {PlayerId} did not receive it.",
                    reward.Kind,
                    trackId,
                    prizeId,
                    playerId
                );

                failed++;

                continue;
            }

            string stepKey = CommerceStepKeys.Indexed(CommerceStepKeys.REWARD_TRACK_GRANT, i);

            try
            {
                bool ok = await granter
                    .GrantAsync(playerId, reward, operation, stepKey, ct)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    failed++;

                    await NoteAsync(operation, stepKey, $"{reward.Kind} refused", ct)
                        .ConfigureAwait(false);

                    continue;
                }

                granted++;
                Append(summary, reward);
            }
            catch (Exception ex)
            {
                // Past the pivot: the claim is already recorded, so this reward is owed. The
                // operation is left at the step that threw, which is what an operator looks for.
                failed++;

                _logger.LogError(
                    ex,
                    "Reward {Index} ({Kind}) of {TrackId}/{PrizeId} failed for player {PlayerId}; it is owed.",
                    i,
                    reward.Kind,
                    trackId,
                    prizeId,
                    playerId
                );

                await NoteAsync(operation, stepKey, ex.Message, ct).ConfigureAwait(false);
            }
        }

        return new RewardBundleResult(granted, failed, summary.ToString());
    }

    private static void Append(StringBuilder summary, RewardGrantSnapshot reward)
    {
        if (summary.Length > 0)
        {
            summary.Append(", ");
        }

        summary
            .Append(reward.Kind.ToString().ToLowerInvariant())
            .Append(':')
            .Append(reward.RewardTypeId)
            .Append('x')
            .Append(reward.Amount);
    }

    private async Task NoteAsync(
        CommerceOperationId operation,
        string stepKey,
        string detail,
        CancellationToken ct
    )
    {
        if (operation.IsNone)
        {
            return;
        }

        try
        {
            // NeedsIntervention, not a failure state that implies compensation: past the pivot the
            // reward is owed, and the only ways out are finishing it or an operator.
            await _journal
                .TransitionAsync(
                    operation,
                    CommerceOperationState.NeedsIntervention,
                    stepKey,
                    detail,
                    ct
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Losing the note is not worth losing the rest of the bundle over.
            _logger.LogError(
                ex,
                "Could not record a failed reward step on {OperationId}.",
                operation
            );
        }
    }
}
