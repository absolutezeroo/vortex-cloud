using System.Collections.Generic;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Players.Polls;

/// <summary>
/// Decides whether a survey may be pushed at a player, and whether they may still answer one. Pure
/// so both branches are testable without a grain.
/// </summary>
public static class PollEligibilityRule
{
    /// <summary>
    /// True when this survey may be offered to a player entering <paramref name="roomId"/>.
    /// A poll with no questions is never offered — the client would open an empty dialog.
    /// </summary>
    /// <param name="poll">The candidate survey.</param>
    /// <param name="roomId">The room the player just entered.</param>
    /// <param name="existingState">
    /// The player's recorded state for this survey, or null when they have never seen it. Any
    /// recorded state blocks a new offer: a pending offer is already on screen, and a declined or
    /// finished survey must not come back.
    /// </param>
    public static bool CanOffer(
        PollDefinitionSnapshot poll,
        int roomId,
        PollParticipationState? existingState
    ) =>
        poll.OfferOnRoomEntry
        && poll.Questions.Length > 0
        && (poll.RoomId is null || poll.RoomId == roomId)
        && existingState is null;

    /// <summary>
    /// True when the player may receive the questions for this survey. Accepting an offer is the
    /// normal path; re-entering a survey already started is allowed (the client can be reopened),
    /// but a declined or completed one is not.
    /// </summary>
    public static bool CanStart(PollParticipationState? existingState) =>
        existingState is null or PollParticipationState.Offered or PollParticipationState.Started;

    /// <summary>
    /// True when an answer to <paramref name="questionId"/> belongs to this survey. Guards against a
    /// client (or a crafted packet) posting answers for questions of another poll.
    /// </summary>
    public static bool OwnsQuestion(PollDefinitionSnapshot poll, int questionId)
    {
        foreach (PollQuestionSnapshot question in poll.Questions)
        {
            if (question.Id == questionId)
            {
                return true;
            }

            foreach (PollQuestionSnapshot child in question.Children)
            {
                if (child.Id == questionId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True once every root question has an answer. Follow-ups are deliberately excluded: which of
    /// them a player sees depends on the choices they made, so requiring them would leave most NPS
    /// surveys permanently unfinished.
    /// </summary>
    public static bool IsComplete(
        PollDefinitionSnapshot poll,
        IReadOnlySet<int> answeredQuestionIds
    )
    {
        if (poll.Questions.Length == 0)
        {
            return false;
        }

        foreach (PollQuestionSnapshot question in poll.Questions)
        {
            if (!answeredQuestionIds.Contains(question.Id))
            {
                return false;
            }
        }

        return true;
    }
}
