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
using Vortex.Primitives.Polls.Grains;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Loads every enabled survey once and caches the assembled question tree for the lifetime of the
/// kept-alive singleton, so per-player poll grains resolve against memory instead of re-reading
/// three tables per request.
/// </summary>
[KeepAlive]
internal sealed class PollManagerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<PollManagerGrain> logger
) : Grain, IPollManagerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PollManagerGrain> _logger = logger;

    private ImmutableArray<PollDefinitionSnapshot> _definitions =
        ImmutableArray<PollDefinitionSnapshot>.Empty;
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<ImmutableArray<PollDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken ct
    )
    {
        if (!_loaded)
        {
            await LoadAsync(ct).ConfigureAwait(true);
        }

        return _definitions;
    }

    public async Task<PollDefinitionSnapshot?> GetDefinitionAsync(int pollId, CancellationToken ct)
    {
        ImmutableArray<PollDefinitionSnapshot> definitions = await GetDefinitionsAsync(ct)
            .ConfigureAwait(true);

        return definitions.FirstOrDefault(p => p.Id == pollId);
    }

    public Task ReloadAsync(CancellationToken ct) => LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PollEntity> polls = await dbCtx
                .Polls.AsNoTracking()
                .Where(p => p.Enabled)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            if (polls.Count == 0)
            {
                _definitions = ImmutableArray<PollDefinitionSnapshot>.Empty;
                _loaded = true;

                return;
            }

            // One query per table rather than per poll: a hotel with a handful of surveys should
            // cost three round-trips on activation, not three per survey.
            List<int> pollIds = [.. polls.Select(p => p.Id)];

            List<PollQuestionEntity> questions = await dbCtx
                .PollQuestions.AsNoTracking()
                .Where(q => pollIds.Contains(q.PollEntityId))
                .OrderBy(q => q.SortOrder)
                .ThenBy(q => q.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<int> questionIds = [.. questions.Select(q => q.Id)];

            List<PollQuestionChoiceEntity> choices = await dbCtx
                .PollQuestionChoices.AsNoTracking()
                .Where(c => questionIds.Contains(c.QuestionEntityId))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            _definitions = Assemble(polls, questions, choices);
            _loaded = true;

            _logger.LogInformation(
                "Loaded {PollCount} poll(s) with {QuestionCount} question(s)",
                _definitions.Length,
                questions.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load poll definitions.");
        }
    }

    private static ImmutableArray<PollDefinitionSnapshot> Assemble(
        List<PollEntity> polls,
        List<PollQuestionEntity> questions,
        List<PollQuestionChoiceEntity> choices
    )
    {
        ILookup<int, PollQuestionChoiceEntity> choicesByQuestion = choices.ToLookup(c =>
            c.QuestionEntityId
        );
        ILookup<int, PollQuestionEntity> childrenByParent = questions
            .Where(q => q.ParentQuestionEntityId is not null)
            .ToLookup(q => q.ParentQuestionEntityId!.Value);
        ILookup<int, PollQuestionEntity> rootsByPoll = questions
            .Where(q => q.ParentQuestionEntityId is null)
            .ToLookup(q => q.PollEntityId);

        return
        [
            .. polls.Select(poll => new PollDefinitionSnapshot
            {
                Id = poll.Id,
                Code = poll.Code,
                PollType = poll.PollType,
                Headline = poll.Headline,
                Summary = poll.Summary,
                StartMessage = poll.StartMessage,
                EndMessage = poll.EndMessage,
                NpsPoll = poll.NpsPoll,
                OfferOnRoomEntry = poll.OfferOnRoomEntry,
                RoomId = poll.RoomEntityId,
                SortOrder = poll.SortOrder,
                Questions =
                [
                    .. rootsByPoll[poll.Id]
                        .Select(root =>
                            ToSnapshot(root, choicesByQuestion, [.. childrenByParent[root.Id]])
                        ),
                ],
            }),
        ];
    }

    private static PollQuestionSnapshot ToSnapshot(
        PollQuestionEntity question,
        ILookup<int, PollQuestionChoiceEntity> choicesByQuestion,
        IReadOnlyList<PollQuestionEntity> children
    ) =>
        new()
        {
            Id = question.Id,
            SortOrder = question.SortOrder,
            QuestionType = question.QuestionType,
            QuestionText = question.QuestionText,
            QuestionCategory = question.QuestionCategory,
            QuestionAnswerType = question.QuestionAnswerType,
            Choices =
            [
                .. choicesByQuestion[question.Id]
                    .Select(c => new PollChoiceSnapshot
                    {
                        Value = c.Value,
                        ChoiceText = c.ChoiceText,
                        ChoiceType = c.ChoiceType,
                    }),
            ],
            Children =
            [
                // A follow-up never carries follow-ups of its own: the client reads exactly one
                // level of nesting, so anything deeper would be written and never read.
                .. children.Select(child => ToSnapshot(child, choicesByQuestion, [])),
            ],
        };
}
