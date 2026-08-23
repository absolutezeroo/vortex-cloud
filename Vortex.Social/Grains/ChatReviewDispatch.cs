using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Help;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Social.Grains;

/// <summary>
/// Turns a chat-review outcome into packets for everyone it touches.
/// </summary>
/// <remarks>
/// Every one of the four packets goes to somebody other than whoever caused it, and the results
/// packet is composed once per recipient rather than once for the group: it carries the reader's own
/// vote beside the verdict, so there is no single packet correct for two people.
/// <para>
/// It lives beside the grain rather than in the handlers because the grain is not the only caller
/// any more -- a review can also end because nobody answered, and that has no packet and no handler
/// behind it. Two senders for one set of outcomes is how the timeout path and the click path drift
/// apart.
/// </para>
/// </remarks>
internal static class ChatReviewDispatch
{
    /// <summary>
    /// What the clients count down. They are told rather than enforced: nothing server-side gives up
    /// on a guardian yet, so a guardian who takes a review and goes quiet holds it open.
    /// </summary>
    private const int AcceptanceTimeoutSeconds = 30;
    private const int VotingTimeoutSeconds = 120;

    public static async Task DeliverAsync(
        IGrainFactory grainFactory,
        ChatReviewOutcome outcome,
        CancellationToken ct
    )
    {
        if (outcome.Nothing)
        {
            return;
        }

        foreach (int guardianId in outcome.OfferedTo)
        {
            await SendAsync(
                    grainFactory,
                    guardianId,
                    new ChatReviewSessionOfferedToGuideMessageComposer
                    {
                        AcceptanceTimeoutSeconds = AcceptanceTimeoutSeconds,
                    }
                )
                .ConfigureAwait(false);
        }

        if (outcome.Result is ChatReviewResultSnapshot result)
        {
            foreach ((int guardianId, int ownVote) in result.VotesByGuardian)
            {
                await SendAsync(
                        grainFactory,
                        guardianId,
                        new ChatReviewSessionResultsMessageComposer
                        {
                            WinningVote = result.WinningVote,
                            OwnVote = ownVote,
                            FinalStatuses = result.Votes,
                        }
                    )
                    .ConfigureAwait(false);
            }

            return;
        }

        ImmutableArray<int> statuses = [.. outcome.Participants];

        foreach (int guardianId in outcome.Participants)
        {
            await SendAsync(
                    grainFactory,
                    guardianId,
                    new ChatReviewSessionVotingStatusMessageComposer { Statuses = statuses }
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>The excerpt, sent to a guardian the moment they take the review.</summary>
    public static Task SendRecordAsync(
        IGrainFactory grainFactory,
        int guardianId,
        string chatRecord
    ) =>
        SendAsync(
            grainFactory,
            guardianId,
            new ChatReviewSessionStartedMessageComposer
            {
                VotingTimeoutSeconds = VotingTimeoutSeconds,
                ChatRecord = chatRecord,
            }
        );

    private static Task SendAsync(
        IGrainFactory grainFactory,
        int playerId,
        Primitives.Networking.IComposer composer
    ) =>
        playerId <= 0
            ? Task.CompletedTask
            : grainFactory.GetPlayerPresenceGrain(playerId).SendComposerAsync(composer);
}
