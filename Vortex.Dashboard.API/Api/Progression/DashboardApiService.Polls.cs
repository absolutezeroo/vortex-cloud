using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Polls;
using Vortex.Primitives.Polls;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read + results surface for surveys. Authoring lives in <c>DashboardOperationsService.Polls.cs</c>;
/// here we only read. There is no separate analytics table — the funnel comes from
/// <c>player_polls</c> and the tallies from <c>player_poll_answers</c>, which stores one row per
/// picked choice, so counting answers is a plain group-by.
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>How many free-text answers a question shows before the page would become a wall.</summary>
    private const int FreeTextAnswerLimit = 50;

    /// <summary>
    /// The question types an operator may pick, with the ones the client's survey dialog actually
    /// renders marked <c>supported</c>. Rating and Binary exist in the client's enum but its content
    /// dialog skips them, so a survey built on those would show the player nothing.
    /// </summary>
    public object PollQuestionTypeOptions()
    {
        var items = Enum.GetValues<PollQuestionType>()
            .Select(type => new
            {
                id = (int)type,
                name = type.ToString(),
                supported = type
                    is PollQuestionType.SingleChoice
                        or PollQuestionType.MultipleChoice
                        or PollQuestionType.TextLine
                        or PollQuestionType.TextArea,
                takesChoices = type
                    is PollQuestionType.SingleChoice
                        or PollQuestionType.MultipleChoice,
            })
            .ToList();

        return new { count = items.Count, items };
    }

    /// <summary>Every survey with its question count and its offer→completion funnel.</summary>
    public Task<object> PollsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                bool enabledOnly = string.Equals(
                    query["enabled"],
                    "true",
                    StringComparison.Ordinal
                );

                IQueryable<PollEntity> pollsQuery = db.Polls.AsNoTracking();

                if (enabledOnly)
                {
                    pollsQuery = pollsQuery.Where(p => p.Enabled);
                }

                var rows = await pollsQuery
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.PollType,
                        p.Headline,
                        p.Summary,
                        p.StartMessage,
                        p.EndMessage,
                        p.NpsPoll,
                        p.Enabled,
                        p.OfferOnRoomEntry,
                        p.RoomEntityId,
                        p.SortOrder,
                        rootQuestionCount = db.PollQuestions.Count(q =>
                            q.PollEntityId == p.Id && q.ParentQuestionEntityId == null
                        ),
                        followUpCount = db.PollQuestions.Count(q =>
                            q.PollEntityId == p.Id && q.ParentQuestionEntityId != null
                        ),
                        offeredCount = db.PlayerPolls.Count(s => s.PollEntityId == p.Id),
                        startedCount = db.PlayerPolls.Count(s =>
                            s.PollEntityId == p.Id
                            && (
                                s.State == PollParticipationState.Started
                                || s.State == PollParticipationState.Completed
                            )
                        ),
                        completedCount = db.PlayerPolls.Count(s =>
                            s.PollEntityId == p.Id && s.State == PollParticipationState.Completed
                        ),
                        rejectedCount = db.PlayerPolls.Count(s =>
                            s.PollEntityId == p.Id && s.State == PollParticipationState.Rejected
                        ),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(
                        db,
                        [
                            .. rows.Where(r => r.RoomEntityId != null)
                                .Select(r => r.RoomEntityId!.Value),
                        ],
                        ct
                    )
                    .ConfigureAwait(false);

                var items = rows.Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.PollType,
                        p.Headline,
                        p.Summary,
                        p.StartMessage,
                        p.EndMessage,
                        p.NpsPoll,
                        p.Enabled,
                        p.OfferOnRoomEntry,
                        roomId = p.RoomEntityId,
                        // A poll pinned to a deleted room never matches anyone: worth seeing here
                        // rather than wondering why the offer stopped appearing.
                        roomName = p.RoomEntityId is { } roomId
                            ? roomNames.GetValueOrDefault(roomId)
                            : null,
                        roomMissing = p.RoomEntityId is { } pinned
                            && !roomNames.ContainsKey(pinned),
                        p.SortOrder,
                        p.rootQuestionCount,
                        p.followUpCount,
                        p.offeredCount,
                        p.startedCount,
                        p.completedCount,
                        p.rejectedCount,
                        completionRate = Share(p.completedCount, p.offeredCount),
                        // A survey with no root question is never offered -- the grain skips it.
                        offerable = p.Enabled && p.rootQuestionCount > 0,
                    })
                    .ToList();

                return new { count = items.Count, items };
            },
            ct
        );

    /// <summary>One survey with its full question tree, choices included, ready to edit.</summary>
    public Task<object?> PollDetailAsync(int pollId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                PollEntity? poll = await db
                    .Polls.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pollId, ct)
                    .ConfigureAwait(false);

                if (poll is null)
                {
                    return null;
                }

                List<PollQuestionEntity> questions = await db
                    .PollQuestions.AsNoTracking()
                    .Where(q => q.PollEntityId == pollId)
                    .OrderBy(q => q.SortOrder)
                    .ThenBy(q => q.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> questionIds = [.. questions.Select(q => q.Id)];

                List<PollQuestionChoiceEntity> choices = await db
                    .PollQuestionChoices.AsNoTracking()
                    .Where(c => questionIds.Contains(c.QuestionEntityId))
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, int> answerCounts = await db
                    .PlayerPollAnswers.AsNoTracking()
                    .Where(a => a.PollEntityId == pollId)
                    .GroupBy(a => a.QuestionEntityId)
                    .Select(g => new { QuestionId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.QuestionId, x => x.Count, ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(
                        db,
                        poll.RoomEntityId is { } pinned ? [pinned] : [],
                        ct
                    )
                    .ConfigureAwait(false);

                ILookup<int, PollQuestionChoiceEntity> choicesByQuestion = choices.ToLookup(c =>
                    c.QuestionEntityId
                );
                ILookup<int, PollQuestionEntity> childrenByParent = questions
                    .Where(q => q.ParentQuestionEntityId is not null)
                    .ToLookup(q => q.ParentQuestionEntityId!.Value);

                List<object> tree = questions
                    .Where(q => q.ParentQuestionEntityId is null)
                    .Select(root =>
                        QuestionNode(
                            root,
                            choicesByQuestion,
                            answerCounts,
                            [
                                .. childrenByParent[root.Id]
                                    .Select(child =>
                                        QuestionNode(child, choicesByQuestion, answerCounts, [])
                                    ),
                            ]
                        )
                    )
                    .ToList();

                return new
                {
                    poll.Id,
                    poll.Code,
                    poll.PollType,
                    poll.Headline,
                    poll.Summary,
                    poll.StartMessage,
                    poll.EndMessage,
                    poll.NpsPoll,
                    poll.Enabled,
                    poll.OfferOnRoomEntry,
                    roomId = poll.RoomEntityId,
                    roomName = poll.RoomEntityId is { } id ? roomNames.GetValueOrDefault(id) : null,
                    poll.SortOrder,
                    questions = tree,
                };
            },
            ct
        );

    /// <summary>
    /// What players actually answered: the participation funnel, then per question either a
    /// per-choice tally or the free text they typed.
    /// </summary>
    public Task<object?> PollResultsAsync(int pollId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                PollEntity? poll = await db
                    .Polls.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pollId, ct)
                    .ConfigureAwait(false);

                if (poll is null)
                {
                    return null;
                }

                List<PollQuestionEntity> questions = await db
                    .PollQuestions.AsNoTracking()
                    .Where(q => q.PollEntityId == pollId)
                    .OrderBy(q => q.ParentQuestionEntityId ?? 0)
                    .ThenBy(q => q.SortOrder)
                    .ThenBy(q => q.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> questionIds = [.. questions.Select(q => q.Id)];

                List<PollQuestionChoiceEntity> choices = await db
                    .PollQuestionChoices.AsNoTracking()
                    .Where(c => questionIds.Contains(c.QuestionEntityId))
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // Answers are stored one row per picked value, so a checkbox question yields several
                // rows for one player -- which is exactly what a per-choice tally wants.
                var answerRows = await db
                    .PlayerPollAnswers.AsNoTracking()
                    .Where(a => a.PollEntityId == pollId)
                    .Select(a => new
                    {
                        a.QuestionEntityId,
                        a.PlayerEntityId,
                        a.Answer,
                        a.AnsweredAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var funnelRows = await db
                    .PlayerPolls.AsNoTracking()
                    .Where(s => s.PollEntityId == pollId)
                    .GroupBy(s => s.State)
                    .Select(g => new { State = g.Key, Count = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<PollParticipationState, int> funnel = funnelRows.ToDictionary(
                    x => x.State,
                    x => x.Count
                );

                int offered = funnel.Values.Sum();
                int completed = funnel.GetValueOrDefault(PollParticipationState.Completed);
                int rejected = funnel.GetValueOrDefault(PollParticipationState.Rejected);
                int started = completed + funnel.GetValueOrDefault(PollParticipationState.Started);
                int pending = funnel.GetValueOrDefault(PollParticipationState.Offered);

                // Only free-text questions need player names, and only for the rows actually shown.
                Dictionary<int, PollQuestionType> typeByQuestion = questions.ToDictionary(
                    q => q.Id,
                    q => q.QuestionType
                );

                List<int> freeTextPlayerIds =
                [
                    .. answerRows
                        .Where(a =>
                            typeByQuestion.TryGetValue(
                                a.QuestionEntityId,
                                out PollQuestionType type
                            ) && type is PollQuestionType.TextLine or PollQuestionType.TextArea
                        )
                        .OrderByDescending(a => a.AnsweredAt)
                        .Select(a => a.PlayerEntityId)
                        .Distinct()
                        .Take(FreeTextAnswerLimit * 4),
                ];

                Dictionary<int, (string Name, string Figure)> players = (
                    await db
                        .Players.AsNoTracking()
                        .Where(p => freeTextPlayerIds.Contains(p.Id))
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Figure,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false)
                ).ToDictionary(p => p.Id, p => (p.Name, p.Figure));

                ILookup<int, PollQuestionChoiceEntity> choicesByQuestion = choices.ToLookup(c =>
                    c.QuestionEntityId
                );
                ILookup<int, string> answersByQuestion = answerRows.ToLookup(
                    a => a.QuestionEntityId,
                    a => a.Answer
                );

                var questionResults = questions
                    .Select(question =>
                    {
                        List<string> given = [.. answersByQuestion[question.Id]];
                        int respondents = answerRows
                            .Where(a => a.QuestionEntityId == question.Id)
                            .Select(a => a.PlayerEntityId)
                            .Distinct()
                            .Count();

                        bool takesChoices =
                            question.QuestionType
                            is PollQuestionType.SingleChoice
                                or PollQuestionType.MultipleChoice;

                        object[] tally = takesChoices
                            ? [.. BuildTally(question, choicesByQuestion, given)]
                            : [];

                        object[] freeText = takesChoices
                            ? []
                            :
                            [
                                .. answerRows
                                    .Where(a => a.QuestionEntityId == question.Id)
                                    .OrderByDescending(a => a.AnsweredAt)
                                    .Take(FreeTextAnswerLimit)
                                    .Select(a =>
                                        (object)
                                            new
                                            {
                                                playerId = a.PlayerEntityId,
                                                playerName = players.TryGetValue(
                                                    a.PlayerEntityId,
                                                    out (string Name, string Figure) player
                                                )
                                                    ? player.Name
                                                    : null,
                                                avatarUrl = players.TryGetValue(
                                                    a.PlayerEntityId,
                                                    out (string Name, string Figure) withFigure
                                                )
                                                    ? _assetUrls.AvatarImage(withFigure.Figure)
                                                    : null,
                                                answer = a.Answer,
                                                a.AnsweredAt,
                                            }
                                    ),
                            ];

                        return new
                        {
                            question.Id,
                            question.QuestionText,
                            questionType = (int)question.QuestionType,
                            questionTypeName = question.QuestionType.ToString(),
                            isFollowUp = question.ParentQuestionEntityId is not null,
                            parentQuestionId = question.ParentQuestionEntityId,
                            question.QuestionCategory,
                            respondents,
                            answerCount = given.Count,
                            tally,
                            freeText,
                            freeTextTruncated = !takesChoices && given.Count > FreeTextAnswerLimit,
                        };
                    })
                    .ToList();

                return new
                {
                    poll.Id,
                    poll.Code,
                    poll.Headline,
                    poll.NpsPoll,
                    funnel = new
                    {
                        offered,
                        pending,
                        started,
                        completed,
                        rejected,
                        completionRate = Share(completed, offered),
                        rejectionRate = Share(rejected, offered),
                    },
                    questions = questionResults,
                };
            },
            ct
        );

    /// <summary>
    /// Counts every configured choice, including the ones nobody picked (a zero is a result), and
    /// appends any answer that no longer matches a configured choice — which is what an edited
    /// question leaves behind, and would otherwise silently vanish from the totals.
    /// </summary>
    private static List<object> BuildTally(
        PollQuestionEntity question,
        ILookup<int, PollQuestionChoiceEntity> choicesByQuestion,
        List<string> given
    )
    {
        Dictionary<string, int> counts = given
            .GroupBy(a => a, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        List<object> tally = [];
        HashSet<string> configured = new(StringComparer.Ordinal);

        foreach (PollQuestionChoiceEntity choice in choicesByQuestion[question.Id])
        {
            configured.Add(choice.Value);
            int count = counts.GetValueOrDefault(choice.Value);

            tally.Add(
                new
                {
                    value = choice.Value,
                    text = choice.ChoiceText,
                    choice.ChoiceType,
                    count,
                    share = Share(count, given.Count),
                    retired = false,
                }
            );
        }

        foreach ((string value, int count) in counts.Where(c => !configured.Contains(c.Key)))
        {
            tally.Add(
                new
                {
                    value,
                    text = value,
                    ChoiceType = 0,
                    count,
                    share = Share(count, given.Count),
                    retired = true,
                }
            );
        }

        return tally;
    }

    private static object QuestionNode(
        PollQuestionEntity question,
        ILookup<int, PollQuestionChoiceEntity> choicesByQuestion,
        Dictionary<int, int> answerCounts,
        object[] children
    ) =>
        new
        {
            question.Id,
            question.SortOrder,
            questionType = (int)question.QuestionType,
            questionTypeName = question.QuestionType.ToString(),
            question.QuestionText,
            question.QuestionCategory,
            question.QuestionAnswerType,
            answerCount = answerCounts.GetValueOrDefault(question.Id),
            choices = choicesByQuestion[question.Id]
                .Select(c => new
                {
                    c.Id,
                    c.Value,
                    c.ChoiceText,
                    c.ChoiceType,
                    c.SortOrder,
                })
                .ToList(),
            children,
        };

    private static double Share(int part, int total) =>
        total <= 0 ? 0 : Math.Round(part * 100d / total, 1);
}
