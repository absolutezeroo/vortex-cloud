using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Creating, changing and removing surveys and their questions.
/// </summary>
/// <remarks>
/// Two dependencies where the service it left took twenty-eight: the runner that audits every write,
/// and the domain's own poll authoring service. Never a direct DB write — <see cref="IPollAdminService"/>
/// reloads the live survey cache after committing, so an edited question is served on the next offer
/// without an emulator restart.
/// </remarks>
internal sealed class PollOperations(OperationRunner runner, IPollAdminService pollAdmin)
{
    private readonly OperationRunner _runner = runner;
    private readonly IPollAdminService _pollAdmin = pollAdmin;

    public Task<OperationResult> CreatePollAsync(
        CreatePollRequest request,
        string actor,
        CancellationToken ct
    ) =>
        _runner.ExecuteAsync(
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
        _runner.ExecuteAsync(
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
        _runner.ExecuteAsync(
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
        _runner.ExecuteAsync(
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
                    .CreateQuestionAsync(ToSpec(request), c)
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
        _runner.ExecuteAsync(
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
                    .UpdateQuestionAsync(request.QuestionId, ToSpec(request), c)
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
        _runner.ExecuteAsync(
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

    private static PollQuestionSpec ToSpec(CreatePollQuestionRequest request) =>
        new(
            request.PollId,
            request.ParentQuestionId,
            request.SortOrder,
            (PollQuestionType)request.QuestionType,
            request.QuestionText,
            request.QuestionCategory,
            request.QuestionAnswerType,
            ToChoices(request.Choices)
        );

    private static PollQuestionSpec ToSpec(UpdatePollQuestionRequest request) =>
        new(
            request.PollId,
            request.ParentQuestionId,
            request.SortOrder,
            (PollQuestionType)request.QuestionType,
            request.QuestionText,
            request.QuestionCategory,
            request.QuestionAnswerType,
            ToChoices(request.Choices)
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
