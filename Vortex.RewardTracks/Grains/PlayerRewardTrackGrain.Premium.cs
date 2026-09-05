using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.RewardTracks;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Wallet;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.RewardTracks.Snapshots;
using Vortex.Protocol.Messages.Outgoing.RewardTracks;

namespace Vortex.RewardTracks.Grains;

/// <summary>
/// Buying premium on a track.
/// </summary>
/// <remarks>
/// Premium belongs to one track. Buying it on the summer campaign does nothing for the introduction
/// track, which is why the entitlement is a column on the player's row for <em>that</em> track and
/// not a flag on the account.
/// </remarks>
internal sealed partial class PlayerRewardTrackGrain
{
    /// <summary>Diamonds, in the client's own activity-point numbering.</summary>
    private const int DiamondsActivityPointType = 5;

    public async Task<RewardPremiumOutcome> PurchasePremiumAsync(
        string trackId,
        CancellationToken ct
    )
    {
        RewardPremiumOutcome outcome = await PurchaseInternalAsync(trackId, ct)
            .ConfigureAwait(true);

        await Presence
            .SendComposerAsync(
                new RewardTrackPremiumPurchaseResultMessageComposer
                {
                    TrackId = outcome.TrackId,
                    Result = outcome.Result,
                    Points = outcome.Points,
                }
            )
            .ConfigureAwait(true);

        if (outcome.Result == RewardPremiumResult.Success)
        {
            // The premium prizes the player already had the points for are now claimable, and the
            // premium tasks now advance. Nothing about that reaches the client from the purchase
            // result alone, so the track list is pushed behind it.
            await PushTracksAsync(reload: false, ct).ConfigureAwait(true);
        }

        return outcome;
    }

    public async Task<bool> GrantPremiumAsync(string trackId, CancellationToken ct)
    {
        if (!catalog.TryGetTrack(trackId, out RewardTrackDefinitionSnapshot? track))
        {
            return false;
        }

        TrackState state = EnsureTrack(trackId);

        if (state.PremiumUnlocked)
        {
            return false;
        }

        await ActivateAsync(track, state, creditsPaid: 0, diamondsPaid: 0, purchased: false, ct)
            .ConfigureAwait(true);

        await PushTracksAsync(reload: false, ct).ConfigureAwait(true);

        return true;
    }

    private async Task<RewardPremiumOutcome> PurchaseInternalAsync(
        string trackId,
        CancellationToken ct
    )
    {
        if (!_featureEnabled)
        {
            return RewardPremiumOutcome.Fail(RewardPremiumResult.Disabled, trackId);
        }

        if (!catalog.TryGetTrack(trackId, out RewardTrackDefinitionSnapshot? track))
        {
            return RewardPremiumOutcome.Fail(RewardPremiumResult.TrackNotFound, trackId);
        }

        DateTime now = DateTime.UtcNow;

        // Selling premium on a track that no longer accepts progress would sell a boost that can
        // never apply.
        if (!track.AcceptsProgressAt(now))
        {
            return RewardPremiumOutcome.Fail(RewardPremiumResult.NotEligible, trackId);
        }

        if (track.Premium is not RewardTrackPremiumSnapshot premium)
        {
            return RewardPremiumOutcome.Fail(RewardPremiumResult.NotConfigured, trackId);
        }

        TrackState state = EnsureTrack(trackId);

        if (state.PremiumUnlocked)
        {
            return RewardPremiumOutcome.Fail(RewardPremiumResult.AlreadyOwned, trackId);
        }

        if (premium.CostCredits <= 0 && premium.CostDiamonds <= 0)
        {
            // Priced at nothing. Refused rather than given away: a track whose premium is free is
            // content somebody has not finished writing, and handing it out would make that
            // unrecoverable.
            return RewardPremiumOutcome.Fail(RewardPremiumResult.InvalidConfiguration, trackId);
        }

        List<WalletDebitRequest> debits = [];

        if (premium.CostCredits > 0)
        {
            debits.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                    Amount = premium.CostCredits,
                }
            );
        }

        if (premium.CostDiamonds > 0)
        {
            debits.Add(
                new WalletDebitRequest
                {
                    CurrencyKind = new CurrencyKind
                    {
                        CurrencyType = CurrencyType.ActivityPoints,
                        ActivityPointType = DiamondsActivityPointType,
                    },
                    Amount = premium.CostDiamonds,
                }
            );
        }

        CommerceOperationId operation = CommerceOperationId.New();

        await journal
            .OpenAsync(
                operation,
                CommerceOperationKind.RewardTrackPremium,
                PlayerId,
                $"track={trackId} credits={premium.CostCredits} diamonds={premium.CostDiamonds}",
                ct
            )
            .ConfigureAwait(true);

        try
        {
            // Both currencies in one debit list, so the wallet takes them together or takes
            // neither. A player is never left short of credits with premium still locked because
            // the diamond half came up short.
            WalletPurchaseResult<bool> result = await grainFactory
                .GetPlayerWalletGrain(PlayerId)
                .ExecutePurchaseAsync(
                    debits,
                    operation,
                    async innerCt =>
                    {
                        await journal
                            .TransitionAsync(
                                operation,
                                CommerceOperationState.Debited,
                                CommerceStepKeys.DEBIT,
                                null,
                                innerCt
                            )
                            .ConfigureAwait(true);

                        await ActivateAsync(
                                track,
                                state,
                                premium.CostCredits,
                                premium.CostDiamonds,
                                purchased: true,
                                innerCt
                            )
                            .ConfigureAwait(true);

                        await journal
                            .TransitionAsync(
                                operation,
                                CommerceOperationState.Completed,
                                CommerceStepKeys.REWARD_TRACK_PREMIUM,
                                null,
                                innerCt
                            )
                            .ConfigureAwait(true);

                        return true;
                    },
                    logger,
                    ct,
                    journal
                )
                .ConfigureAwait(true);

            if (!result.Succeeded)
            {
                await journal
                    .TransitionAsync(
                        operation,
                        CommerceOperationState.FailedBeforePivot,
                        CommerceStepKeys.DEBIT,
                        "insufficient balance",
                        ct
                    )
                    .ConfigureAwait(true);

                // Which currency ran out, so the client shows the right line. The wallet reports
                // the first failing request, and "not enough diamonds" beats a generic refusal when
                // the player is staring at a credit balance that is plainly sufficient.
                RewardPremiumResult reason =
                    result.Failure?.CurrencyKind.CurrencyType == CurrencyType.ActivityPoints
                        ? RewardPremiumResult.NotEnoughDiamonds
                        : RewardPremiumResult.NotEnoughCredits;

                return RewardPremiumOutcome.Fail(reason, trackId);
            }
        }
        catch (Exception ex)
        {
            // The executor has already put the money back and recorded it.
            logger.LogError(
                ex,
                "Premium purchase on {TrackId} failed after the debit for player {PlayerId}; the balance was refunded.",
                trackId,
                PlayerId
            );

            return RewardPremiumOutcome.Fail(RewardPremiumResult.Failed, trackId);
        }

        return new RewardPremiumOutcome
        {
            Result = RewardPremiumResult.Success,
            TrackId = trackId,
            Points = state.Points,
        };
    }

    /// <summary>
    /// Turns premium on and credits the instant points, in one commit.
    /// </summary>
    /// <remarks>
    /// The instant points are added to the total rather than boosted by the multiplier they come
    /// with: they are a fixed advance stated in the offer ("instantly gain N points"), not work the
    /// boost applies to.
    /// </remarks>
    private async Task ActivateAsync(
        RewardTrackDefinitionSnapshot track,
        TrackState state,
        int creditsPaid,
        int diamondsPaid,
        bool purchased,
        CancellationToken ct
    )
    {
        DateTime now = DateTime.UtcNow;
        int instant = Math.Max(0, track.Premium?.InstantPoints ?? 0);

        state.PremiumUnlocked = true;
        state.PremiumUnlockedAt = now;
        state.Points += instant;
        state.ContentVersion = track.ContentVersion;

        await using (
            VortexDbContext db = await dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true)
        )
        {
            PlayerRewardTrackEntity? row = await db
                .PlayerRewardTracks.FirstOrDefaultAsync(
                    t => t.PlayerEntityId == PlayerId && t.TrackId == track.TrackId,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                db.PlayerRewardTracks.Add(
                    new PlayerRewardTrackEntity
                    {
                        PlayerEntityId = PlayerId,
                        TrackId = track.TrackId,
                        Points = state.Points,
                        PremiumUnlocked = true,
                        PremiumUnlockedAt = now,
                        ContentVersion = state.ContentVersion,
                    }
                );
            }
            else
            {
                row.PremiumUnlocked = true;
                row.PremiumUnlockedAt = now;
                row.Points = state.Points;
                row.ContentVersion = state.ContentVersion;
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(true);
        }

        await events
            .PublishAsync(
                new RewardTrackPremiumActivatedEvent(
                    PlayerId,
                    track.TrackId,
                    purchased,
                    creditsPaid,
                    diamondsPaid
                ),
                ct
            )
            .ConfigureAwait(true);

        logger.LogInformation(
            "Premium activated on {TrackId} for player {PlayerId} (purchased={Purchased}, {Credits}c {Diamonds}d, +{Instant} instant point(s), total {Total}).",
            track.TrackId,
            PlayerId,
            purchased,
            creditsPaid,
            diamondsPaid,
            instant,
            state.Points
        );

        await CheckCompletionAsync(track, state, now, ct).ConfigureAwait(true);
    }
}
