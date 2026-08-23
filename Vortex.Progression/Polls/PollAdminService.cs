using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Polls;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Admin;

namespace Vortex.Players.Polls;

/// <summary>
/// CRUD for the survey tables. A plain singleton (not a grain) opening a short-lived
/// <see cref="VortexDbContext"/> per call: poll rows aren't grain-owned and admin writes are rare.
/// The surveys players are offered come from the kept-alive <see cref="Grains.PollManagerGrain"/>
/// cache, which is only rebuilt via its <c>ReloadAsync</c>; every write here reloads it afterwards
/// so the DB and the live cache never drift — the "DB write not reflected in live state" bug class
/// called out in AGENTS.md.
/// </summary>
internal sealed class PollAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ILogger<PollAdminService> logger
) : IPollAdminService
{
    public async Task<PollAdminResult> CreatePollAsync(PollSpec spec, CancellationToken ct)
    {
        if (PollAuthoringRules.ValidatePoll(spec) is { } error)
        {
            return PollAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string code = spec.Code.Trim();

        if (await db.Polls.AnyAsync(p => p.Code == code, ct).ConfigureAwait(false))
        {
            return PollAdminResult.Fail("poll_code_taken");
        }

        PollEntity entity = new()
        {
            Code = code,
            PollType = (spec.PollType ?? string.Empty).Trim(),
            Headline = spec.Headline.Trim(),
            Summary = spec.Summary.Trim(),
            StartMessage = (spec.StartMessage ?? string.Empty).Trim(),
            EndMessage = (spec.EndMessage ?? string.Empty).Trim(),
            NpsPoll = spec.NpsPoll,
            Enabled = spec.Enabled,
            OfferOnRoomEntry = spec.OfferOnRoomEntry,
            RoomEntityId = spec.RoomId,
            SortOrder = spec.SortOrder,
        };

        db.Polls.Add(entity);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return PollAdminResult.Ok(entity.Id);
    }

    public async Task<PollAdminResult> UpdatePollAsync(
        int pollId,
        PollSpec spec,
        CancellationToken ct
    )
    {
        if (PollAuthoringRules.ValidatePoll(spec) is { } error)
        {
            return PollAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PollEntity? entity = await db
            .Polls.FirstOrDefaultAsync(p => p.Id == pollId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return PollAdminResult.Fail("poll_not_found");
        }

        string code = spec.Code.Trim();

        if (
            await db.Polls.AnyAsync(p => p.Code == code && p.Id != pollId, ct).ConfigureAwait(false)
        )
        {
            return PollAdminResult.Fail("poll_code_taken");
        }

        entity.Code = code;
        entity.PollType = (spec.PollType ?? string.Empty).Trim();
        entity.Headline = spec.Headline.Trim();
        entity.Summary = spec.Summary.Trim();
        entity.StartMessage = (spec.StartMessage ?? string.Empty).Trim();
        entity.EndMessage = (spec.EndMessage ?? string.Empty).Trim();
        entity.NpsPoll = spec.NpsPoll;
        entity.Enabled = spec.Enabled;
        entity.OfferOnRoomEntry = spec.OfferOnRoomEntry;
        entity.RoomEntityId = spec.RoomId;
        entity.SortOrder = spec.SortOrder;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return PollAdminResult.Ok(entity.Id);
    }

    public async Task<PollAdminResult> DeletePollAsync(int pollId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PollEntity? entity = await db
            .Polls.FirstOrDefaultAsync(p => p.Id == pollId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return PollAdminResult.Fail("poll_not_found");
        }

        // Participation rows reference the poll non-cascade on purpose: a survey with results is a
        // record, not a draft. Steer the operator to disabling it, the same guard the quest admin
        // uses for quests with progress.
        bool hasParticipation = await db
            .PlayerPolls.AnyAsync(p => p.PollEntityId == pollId, ct)
            .ConfigureAwait(false);

        if (hasParticipation)
        {
            return PollAdminResult.Fail("poll_has_answers");
        }

        // Questions and choices cascade with the poll in the model, so removing the poll is enough.
        db.Polls.Remove(entity);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return PollAdminResult.Ok(pollId);
    }

    public async Task<PollAdminResult> CreateQuestionAsync(
        PollQuestionSpec spec,
        CancellationToken ct
    )
    {
        if (PollAuthoringRules.ValidateQuestion(spec) is { } error)
        {
            return PollAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (!await db.Polls.AnyAsync(p => p.Id == spec.PollId, ct).ConfigureAwait(false))
        {
            return PollAdminResult.Fail("poll_not_found");
        }

        if (
            await ResolveParentErrorAsync(db, spec, questionId: null, ct).ConfigureAwait(false) is
            { } parentError
        )
        {
            return PollAdminResult.Fail(parentError);
        }

        PollQuestionEntity entity = new()
        {
            PollEntityId = spec.PollId,
            ParentQuestionEntityId = spec.ParentQuestionId,
            SortOrder = spec.SortOrder,
            QuestionType = spec.QuestionType,
            QuestionText = spec.QuestionText.Trim(),
            QuestionCategory = spec.QuestionCategory,
            QuestionAnswerType = spec.QuestionAnswerType,
        };

        db.PollQuestions.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        ApplyChoices(db, entity.Id, spec, existing: []);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return PollAdminResult.Ok(entity.Id);
    }

    public async Task<PollAdminResult> UpdateQuestionAsync(
        int questionId,
        PollQuestionSpec spec,
        CancellationToken ct
    )
    {
        if (PollAuthoringRules.ValidateQuestion(spec) is { } error)
        {
            return PollAdminResult.Fail(error);
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PollQuestionEntity? entity = await db
            .PollQuestions.FirstOrDefaultAsync(q => q.Id == questionId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return PollAdminResult.Fail("question_not_found");
        }

        if (
            await ResolveParentErrorAsync(db, spec, questionId, ct).ConfigureAwait(false) is
            { } parentError
        )
        {
            return PollAdminResult.Fail(parentError);
        }

        // Turning a root question into a follow-up would strand its own follow-ups one level too
        // deep, where the client never reads them.
        if (
            spec.ParentQuestionId is not null
            && await db
                .PollQuestions.AnyAsync(q => q.ParentQuestionEntityId == questionId, ct)
                .ConfigureAwait(false)
        )
        {
            return PollAdminResult.Fail("question_has_follow_ups");
        }

        entity.PollEntityId = spec.PollId;
        entity.ParentQuestionEntityId = spec.ParentQuestionId;
        entity.SortOrder = spec.SortOrder;
        entity.QuestionType = spec.QuestionType;
        entity.QuestionText = spec.QuestionText.Trim();
        entity.QuestionCategory = spec.QuestionCategory;
        entity.QuestionAnswerType = spec.QuestionAnswerType;

        List<PollQuestionChoiceEntity> existing = await db
            .PollQuestionChoices.Where(c => c.QuestionEntityId == questionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ApplyChoices(db, questionId, spec, existing);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return PollAdminResult.Ok(questionId);
    }

    public async Task<PollAdminResult> DeleteQuestionAsync(int questionId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PollQuestionEntity? entity = await db
            .PollQuestions.FirstOrDefaultAsync(q => q.Id == questionId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return PollAdminResult.Fail("question_not_found");
        }

        bool hasAnswers = await db
            .PlayerPollAnswers.AnyAsync(a => a.QuestionEntityId == questionId, ct)
            .ConfigureAwait(false);

        if (hasAnswers)
        {
            return PollAdminResult.Fail("question_has_answers");
        }

        // The follow-up link is Restrict (MySQL rejects a cascading self-FK), so children must go
        // first or the delete fails at the database with a far less readable message.
        bool hasFollowUps = await db
            .PollQuestions.AnyAsync(q => q.ParentQuestionEntityId == questionId, ct)
            .ConfigureAwait(false);

        if (hasFollowUps)
        {
            return PollAdminResult.Fail("question_has_follow_ups");
        }

        List<PollQuestionChoiceEntity> choices = await db
            .PollQuestionChoices.Where(c => c.QuestionEntityId == questionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Tracked removes rather than ExecuteDeleteAsync so the choices and the question land in one
        // SaveChangesAsync: a half-applied delete would leave choices pointing at nothing.
        db.PollQuestionChoices.RemoveRange(choices);
        db.PollQuestions.Remove(entity);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return PollAdminResult.Ok(questionId);
    }

    /// <summary>
    /// Replaces the question's choice list with the spec's. Answers store the picked value as text,
    /// never a choice id, so dropping a choice here does not touch a single recorded answer.
    /// </summary>
    private static void ApplyChoices(
        VortexDbContext db,
        int questionId,
        PollQuestionSpec spec,
        List<PollQuestionChoiceEntity> existing
    )
    {
        if (existing.Count > 0)
        {
            db.PollQuestionChoices.RemoveRange(existing);
        }

        if (!PollAuthoringRules.TakesChoices(spec.QuestionType))
        {
            // A text question's choices are never written to the wire; keeping them in the table
            // would only be a trap for whoever edits the question next.
            return;
        }

        int order = 0;

        foreach (PollChoiceSpec choice in spec.Choices)
        {
            db.PollQuestionChoices.Add(
                new PollQuestionChoiceEntity
                {
                    QuestionEntityId = questionId,
                    Value = choice.Value.Trim(),
                    ChoiceText = choice.ChoiceText.Trim(),
                    ChoiceType = choice.ChoiceType,
                    SortOrder = choice.SortOrder == 0 ? order : choice.SortOrder,
                }
            );

            order++;
        }
    }

    private static async Task<string?> ResolveParentErrorAsync(
        VortexDbContext db,
        PollQuestionSpec spec,
        int? questionId,
        CancellationToken ct
    )
    {
        if (spec.ParentQuestionId is not { } parentId)
        {
            return null;
        }

        if (parentId == questionId)
        {
            return "question_parent_self";
        }

        PollQuestionEntity? parent = await db
            .PollQuestions.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == parentId, ct)
            .ConfigureAwait(false);

        if (parent is null)
        {
            return "question_parent_not_found";
        }

        if (parent.PollEntityId != spec.PollId)
        {
            return "question_parent_other_poll";
        }

        // The client reads exactly one level of nesting: a follow-up of a follow-up is written and
        // never shown.
        return parent.ParentQuestionEntityId is null ? null : "question_parent_not_root";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetPollManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The DB write already committed -- the live poll cache is now stale until the next
            // reload or restart. Never swallow this: it is the "DB write not reflected in live
            // state" bug class called out in AGENTS.md.
            logger.LogError(
                ex,
                "Poll cache reload failed after an admin write committed -- live surveys are now stale until the next reload or restart"
            );

            throw;
        }
    }
}
