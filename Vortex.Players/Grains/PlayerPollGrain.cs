using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Polls;
using Vortex.Players.Polls;
using Vortex.Protocol.Messages.Outgoing.Poll;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Grains;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Per-player survey grain. Owns the player's participation rows and their answers, and sends every
/// poll composer itself — the handlers that call it only forward the request.
/// </summary>
internal sealed class PlayerPollGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<PlayerPollGrain> logger
) : Grain, IPlayerPollGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PlayerPollGrain> _logger = logger;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    private IPlayerPresenceGrain Presence => _grainFactory.GetPlayerPresenceGrain(PlayerId);

    public async Task OfferForRoomEntryAsync(int roomId, CancellationToken ct)
    {
        ImmutableArray<PollDefinitionSnapshot> definitions = await _grainFactory
            .GetPollManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        if (definitions.Length == 0)
        {
            return;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        Dictionary<int, PollParticipationState> states = await LoadStatesAsync(dbCtx, ct)
            .ConfigureAwait(true);

        PollDefinitionSnapshot? candidate = definitions.FirstOrDefault(poll =>
            PollEligibilityRule.CanOffer(
                poll,
                roomId,
                states.TryGetValue(poll.Id, out PollParticipationState state) ? state : null
            )
        );

        if (candidate is null)
        {
            return;
        }

        dbCtx.PlayerPolls.Add(
            new PlayerPollEntity
            {
                PlayerEntityId = PlayerId,
                PollEntityId = candidate.Id,
                State = PollParticipationState.Offered,
                OfferedAt = DateTime.UtcNow,
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await Presence
            .SendComposerAsync(
                new PollOfferEventMessageComposer
                {
                    PollId = candidate.Id,
                    PollType = candidate.PollType,
                    Headline = candidate.Headline,
                    Summary = candidate.Summary,
                }
            )
            .ConfigureAwait(true);
    }

    public async Task StartAsync(int pollId, CancellationToken ct)
    {
        PollDefinitionSnapshot? poll = await _grainFactory
            .GetPollManagerGrain()
            .GetDefinitionAsync(pollId, ct)
            .ConfigureAwait(true);

        if (poll is null || poll.Questions.Length == 0)
        {
            await SendErrorAsync().ConfigureAwait(true);

            return;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        PlayerPollEntity? participation = await dbCtx
            .PlayerPolls.FirstOrDefaultAsync(
                p => p.PlayerEntityId == PlayerId && p.PollEntityId == pollId,
                ct
            )
            .ConfigureAwait(true);

        if (!PollEligibilityRule.CanStart(participation?.State))
        {
            await SendErrorAsync().ConfigureAwait(true);

            return;
        }

        if (participation is null)
        {
            // The survey was reached without an offer (a re-opened widget, or a poll started by
            // something other than room entry). Record the start rather than dropping it.
            dbCtx.PlayerPolls.Add(
                new PlayerPollEntity
                {
                    PlayerEntityId = PlayerId,
                    PollEntityId = pollId,
                    State = PollParticipationState.Started,
                    StartedAt = DateTime.UtcNow,
                }
            );
        }
        else if (participation.State == PollParticipationState.Offered)
        {
            participation.State = PollParticipationState.Started;
            participation.StartedAt = DateTime.UtcNow;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await Presence
            .SendComposerAsync(
                new PollContentsEventMessageComposer
                {
                    PollId = poll.Id,
                    StartMessage = poll.StartMessage,
                    EndMessage = poll.EndMessage,
                    Questions = poll.Questions,
                    NpsPoll = poll.NpsPoll,
                }
            )
            .ConfigureAwait(true);
    }

    public async Task RejectAsync(int pollId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        PlayerPollEntity? participation = await dbCtx
            .PlayerPolls.FirstOrDefaultAsync(
                p => p.PlayerEntityId == PlayerId && p.PollEntityId == pollId,
                ct
            )
            .ConfigureAwait(true);

        if (participation is null)
        {
            dbCtx.PlayerPolls.Add(
                new PlayerPollEntity
                {
                    PlayerEntityId = PlayerId,
                    PollEntityId = pollId,
                    State = PollParticipationState.Rejected,
                    FinishedAt = DateTime.UtcNow,
                }
            );
        }
        else if (participation.State is PollParticipationState.Offered)
        {
            participation.State = PollParticipationState.Rejected;
            participation.FinishedAt = DateTime.UtcNow;
        }
        else
        {
            // Declining a survey already started or finished is a no-op: the answers stay, and a
            // completed survey must not fall back to "rejected".
            return;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
    }

    public async Task AnswerAsync(
        int pollId,
        int questionId,
        ImmutableArray<string> answers,
        CancellationToken ct
    )
    {
        PollDefinitionSnapshot? poll = await _grainFactory
            .GetPollManagerGrain()
            .GetDefinitionAsync(pollId, ct)
            .ConfigureAwait(true);

        if (poll is null || !PollEligibilityRule.OwnsQuestion(poll, questionId))
        {
            _logger.LogWarning(
                "Player {PlayerId} answered question {QuestionId}, which does not belong to poll {PollId}.",
                PlayerId,
                questionId,
                pollId
            );

            return;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        PlayerPollEntity? participation = await dbCtx
            .PlayerPolls.FirstOrDefaultAsync(
                p => p.PlayerEntityId == PlayerId && p.PollEntityId == pollId,
                ct
            )
            .ConfigureAwait(true);

        if (participation is null || participation.State == PollParticipationState.Rejected)
        {
            _logger.LogWarning(
                "Player {PlayerId} answered poll {PollId} they never started.",
                PlayerId,
                pollId
            );

            return;
        }

        // Re-answering a question replaces the earlier answer instead of stacking a second one.
        // Tracked removes rather than ExecuteDeleteAsync so the replace and the insert land in the
        // same SaveChangesAsync.
        List<PlayerPollAnswerEntity> previous = await dbCtx
            .PlayerPollAnswers.Where(a =>
                a.PlayerEntityId == PlayerId
                && a.PollEntityId == pollId
                && a.QuestionEntityId == questionId
            )
            .ToListAsync(ct)
            .ConfigureAwait(true);

        dbCtx.PlayerPollAnswers.RemoveRange(previous);

        DateTime answeredAt = DateTime.UtcNow;

        foreach (string answer in answers)
        {
            dbCtx.PlayerPollAnswers.Add(
                new PlayerPollAnswerEntity
                {
                    PlayerEntityId = PlayerId,
                    PollEntityId = pollId,
                    QuestionEntityId = questionId,
                    Answer = answer,
                    AnsweredAt = answeredAt,
                }
            );
        }

        if (participation.State == PollParticipationState.Offered)
        {
            // An answer proves the survey is open even if the start was never recorded.
            participation.State = PollParticipationState.Started;
            participation.StartedAt ??= answeredAt;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await CompleteIfFinishedAsync(dbCtx, poll, participation, answeredAt, ct)
            .ConfigureAwait(true);
    }

    private async Task CompleteIfFinishedAsync(
        VortexDbContext dbCtx,
        PollDefinitionSnapshot poll,
        PlayerPollEntity participation,
        DateTime answeredAt,
        CancellationToken ct
    )
    {
        if (participation.State == PollParticipationState.Completed)
        {
            return;
        }

        HashSet<int> answeredQuestionIds =
        [
            .. await dbCtx
                .PlayerPollAnswers.AsNoTracking()
                .Where(a => a.PlayerEntityId == PlayerId && a.PollEntityId == poll.Id)
                .Select(a => a.QuestionEntityId)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(true),
        ];

        if (!PollEligibilityRule.IsComplete(poll, answeredQuestionIds))
        {
            return;
        }

        participation.State = PollParticipationState.Completed;
        participation.FinishedAt = answeredAt;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        _logger.LogInformation("Player {PlayerId} completed poll {PollCode}.", PlayerId, poll.Code);
    }

    private async Task<Dictionary<int, PollParticipationState>> LoadStatesAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    ) =>
        await dbCtx
            .PlayerPolls.AsNoTracking()
            .Where(p => p.PlayerEntityId == PlayerId)
            .ToDictionaryAsync(p => p.PollEntityId, p => p.State, ct)
            .ConfigureAwait(true);

    private Task SendErrorAsync() =>
        Presence.SendComposerAsync(new PollErrorEventMessageComposer());
}
