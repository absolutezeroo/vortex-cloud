using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Poll admin operations. Each routes through <see cref="IPollAdminService"/> (never a direct DB
/// write), which reloads the live survey cache after committing, and emits a durable audit event
/// with the operator's reason — same contract as the quest operations.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreatePollAsync(
        CreatePollRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.poll.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: request.RoomId,
            detail: new
            {
                request.Code,
                request.Headline,
                request.NpsPoll,
                request.Enabled,
            },
            work: async c =>
            {
                PollAdminResult result = await _pollAdmin
                    .CreatePollAsync(ToSpec(request), c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> UpdatePollAsync(
        UpdatePollRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.poll.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: request.RoomId,
            detail: new
            {
                request.PollId,
                request.Code,
                request.Headline,
                request.Enabled,
            },
            work: async c =>
            {
                PollAdminResult result = await _pollAdmin
                    .UpdatePollAsync(request.PollId, ToSpec(request), c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeletePollAsync(
        DeletePollRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.poll.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.PollId },
            work: async c =>
            {
                PollAdminResult result = await _pollAdmin
                    .DeletePollAsync(request.PollId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> CreatePollQuestionAsync(
        CreatePollQuestionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.poll.question.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.PollId,
                request.ParentQuestionId,
                request.QuestionType,
                choiceCount = request.Choices?.Count ?? 0,
            },
            work: async c =>
            {
                PollAdminResult result = await _pollAdmin
                    .CreateQuestionAsync(
                        new PollQuestionSpec(
                            request.PollId,
                            request.ParentQuestionId,
                            request.SortOrder,
                            (PollQuestionType)request.QuestionType,
                            request.QuestionText,
                            request.QuestionCategory,
                            request.QuestionAnswerType,
                            ToChoices(request.Choices)
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> UpdatePollQuestionAsync(
        UpdatePollQuestionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.poll.question.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.QuestionId,
                request.PollId,
                request.QuestionType,
                choiceCount = request.Choices?.Count ?? 0,
            },
            work: async c =>
            {
                PollAdminResult result = await _pollAdmin
                    .UpdateQuestionAsync(
                        request.QuestionId,
                        new PollQuestionSpec(
                            request.PollId,
                            request.ParentQuestionId,
                            request.SortOrder,
                            (PollQuestionType)request.QuestionType,
                            request.QuestionText,
                            request.QuestionCategory,
                            request.QuestionAnswerType,
                            ToChoices(request.Choices)
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeletePollQuestionAsync(
        DeletePollQuestionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.poll.question.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.QuestionId },
            work: async c =>
            {
                PollAdminResult result = await _pollAdmin
                    .DeleteQuestionAsync(request.QuestionId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    private static PollSpec ToSpec(CreatePollRequest request) =>
        new(
            request.Code,
            request.PollType,
            request.Headline,
            request.Summary,
            request.StartMessage,
            request.EndMessage,
            request.NpsPoll,
            request.Enabled,
            request.OfferOnRoomEntry,
            request.RoomId,
            request.SortOrder
        );

    private static PollSpec ToSpec(UpdatePollRequest request) =>
        new(
            request.Code,
            request.PollType,
            request.Headline,
            request.Summary,
            request.StartMessage,
            request.EndMessage,
            request.NpsPoll,
            request.Enabled,
            request.OfferOnRoomEntry,
            request.RoomId,
            request.SortOrder
        );

    private static IReadOnlyList<PollChoiceSpec> ToChoices(
        IReadOnlyList<PollChoiceBody>? choices
    ) =>
        choices is null
            ? []
            :
            [
                .. choices.Select(c => new PollChoiceSpec(
                    c.Value,
                    c.ChoiceText,
                    c.ChoiceType,
                    c.SortOrder
                )),
            ];
}
