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
using Vortex.Database.Entities.Help;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Social.Help;

namespace Vortex.Social.Grains;

/// <summary>
/// Per-player quiz grain. The answer key stays here: the client is sent question numbers only, and
/// grades nothing itself.
/// </summary>
internal sealed class PlayerQuizGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IEventPublisher events,
    ILogger<PlayerQuizGrain> logger
) : Grain, IPlayerQuizGrain
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<PlayerQuizGrain> _logger = logger;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    private IPlayerPresenceGrain Presence => _grainFactory.GetPlayerPresenceGrain((long)PlayerId);

    public async Task RequestAsync(string quizCode, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            QuizEntity? quiz = await LoadQuizAsync(dbCtx, quizCode, ct).ConfigureAwait(true);

            if (quiz is null)
            {
                return;
            }

            List<QuizQuestionEntity> questions = await LoadQuestionsAsync(dbCtx, quiz.Id, ct)
                .ConfigureAwait(true);

            if (questions.Count == 0)
            {
                // The client opens its window on this message and then asks for question zero. With
                // no questions it would show an empty modal the player cannot leave except by
                // closing it, so a quiz with no rows simply does not open.
                return;
            }

            await Presence
                .SendComposerAsync(
                    new QuizDataMessageComposer
                    {
                        QuizCode = quiz.Code,
                        QuestionIds = [.. questions.Select(q => q.QuestionNumber)],
                    }
                )
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send quiz {QuizCode} to player {PlayerId}.",
                quizCode,
                PlayerId
            );
        }
    }

    public async Task SubmitAsync(
        string quizCode,
        ImmutableArray<int> answers,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            QuizEntity? quiz = await LoadQuizAsync(dbCtx, quizCode, ct).ConfigureAwait(true);

            if (quiz is null)
            {
                return;
            }

            List<QuizQuestionEntity> questions = await LoadQuestionsAsync(dbCtx, quiz.Id, ct)
                .ConfigureAwait(true);

            if (questions.Count == 0)
            {
                return;
            }

            // Graded against the same order RequestAsync sent, which is why both go through
            // LoadQuestionsAsync rather than each picking their own ordering.
            ImmutableArray<int> wrong = QuizGrader.Grade(
                [.. questions.Select(q => (q.QuestionNumber, q.CorrectAnswerIndex))],
                answers
            );

            bool passed = wrong.IsEmpty;
            bool firstPass = await RecordAttemptAsync(dbCtx, quiz.Id, passed, ct)
                .ConfigureAwait(true);

            if (firstPass && !string.IsNullOrEmpty(quiz.RewardBadgeCode))
            {
                await _grainFactory
                    .GetInventoryGrain(PlayerId)
                    .GrantBadgeAsync(quiz.RewardBadgeCode, ct)
                    .ConfigureAwait(true);
            }

            await Presence
                .SendComposerAsync(
                    new QuizResultsMessageComposer
                    {
                        QuizCode = quiz.Code,
                        WrongQuestionIds = wrong,
                    }
                )
                .ConfigureAwait(true);

            await _events
                .PublishAsync(new QuizSubmittedEvent(PlayerId, quiz.Code), ct)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to grade quiz {QuizCode} for player {PlayerId}.",
                quizCode,
                PlayerId
            );
        }
    }

    private static Task<QuizEntity?> LoadQuizAsync(
        VortexDbContext dbCtx,
        string quizCode,
        CancellationToken ct
    ) => dbCtx.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Enabled && q.Code == quizCode, ct);

    /// <summary>The quiz's questions in one fixed order, shared by the send and the grade — the
    /// answers come back positionally, so the two must not order differently.</summary>
    private static Task<List<QuizQuestionEntity>> LoadQuestionsAsync(
        VortexDbContext dbCtx,
        int quizId,
        CancellationToken ct
    ) =>
        dbCtx
            .QuizQuestions.AsNoTracking()
            .Where(q => q.QuizEntityId == quizId)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.QuestionNumber)
            .ToListAsync(ct);

    /// <summary>Counts the attempt and returns whether this was the first pass — the moment the
    /// badge is due, and only that moment.</summary>
    private async Task<bool> RecordAttemptAsync(
        VortexDbContext dbCtx,
        int quizId,
        bool passed,
        CancellationToken ct
    )
    {
        PlayerQuizEntity? row = await dbCtx
            .PlayerQuizzes.FirstOrDefaultAsync(
                p => p.PlayerEntityId == PlayerId && p.QuizEntityId == quizId,
                ct
            )
            .ConfigureAwait(true);

        if (row is null)
        {
            row = new PlayerQuizEntity { PlayerEntityId = PlayerId, QuizEntityId = quizId };
            dbCtx.PlayerQuizzes.Add(row);
        }

        row.Attempts++;

        bool firstPass = passed && row.PassedAt is null;

        if (firstPass)
        {
            row.PassedAt = DateTime.UtcNow;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        return firstPass;
    }
}
