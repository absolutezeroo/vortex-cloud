using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Api.Progression;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Operations.Progression;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Poll admin surface: read (list/detail), the answers players gave (results), and the CRUD
/// operations that edit a survey live — every write reloads the in-memory poll cache so an edited
/// question is served on the next offer without an emulator restart (see <c>IPollAdminService</c>).
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagPolls = "Polls";
    private const string ApiPolls = ApiV1 + "/polls";

    public static void MapPollReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiPolls,
            (HttpContext ctx, PollReads polls, CancellationToken ct) =>
                OkAsync(polls.PollsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PollsRead,
            TagPolls
        );
        MapReadGet(
            app,
            ApiPolls + "/question-types",
            (PollReads polls) => Results.Ok(polls.PollQuestionTypeOptions()),
            Capabilities.Dashboard.PollsRead,
            TagPolls
        );
        MapReadGet(
            app,
            ApiPolls + "/{pollId:int}",
            (int pollId, PollReads polls, CancellationToken ct) =>
                OkNullableAsync(polls.PollDetailAsync(pollId, ct)),
            Capabilities.Dashboard.PollsRead,
            TagPolls
        );
        MapReadGet(
            app,
            ApiPolls + "/{pollId:int}/results",
            (int pollId, PollReads polls, CancellationToken ct) =>
                OkNullableAsync(polls.PollResultsAsync(pollId, ct)),
            Capabilities.Dashboard.PollsRead,
            TagPolls
        );
    }

    public static void MapPollOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/polls",
            async (
                HttpContext ctx,
                CreatePollRequest body,
                PollOperations ops,
                CancellationToken ct
            ) =>
            {
                if (
                    string.IsNullOrWhiteSpace(body.Code)
                    || string.IsNullOrWhiteSpace(body.Headline)
                    || string.IsNullOrWhiteSpace(body.Summary)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreatePollAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsPollsManage,
            TagPolls
        );
        MapPost(
            app,
            ApiOperations + "/polls/update",
            async (
                HttpContext ctx,
                UpdatePollRequest body,
                PollOperations ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body.PollId <= 0
                    || string.IsNullOrWhiteSpace(body.Code)
                    || string.IsNullOrWhiteSpace(body.Headline)
                    || string.IsNullOrWhiteSpace(body.Summary)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdatePollAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsPollsManage,
            TagPolls
        );
        MapPost(
            app,
            ApiOperations + "/polls/delete",
            async (
                HttpContext ctx,
                DeletePollRequest body,
                PollOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.PollId <= 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeletePollAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsPollsManage,
            TagPolls
        );
        MapPost(
            app,
            ApiOperations + "/polls/questions",
            async (
                HttpContext ctx,
                CreatePollQuestionRequest body,
                PollOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.PollId <= 0 || string.IsNullOrWhiteSpace(body.QuestionText))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreatePollQuestionAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsPollsManage,
            TagPolls
        );
        MapPost(
            app,
            ApiOperations + "/polls/questions/update",
            async (
                HttpContext ctx,
                UpdatePollQuestionRequest body,
                PollOperations ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body.QuestionId <= 0
                    || body.PollId <= 0
                    || string.IsNullOrWhiteSpace(body.QuestionText)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdatePollQuestionAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsPollsManage,
            TagPolls
        );
        MapPost(
            app,
            ApiOperations + "/polls/questions/delete",
            async (
                HttpContext ctx,
                DeletePollQuestionRequest body,
                PollOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.QuestionId <= 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeletePollQuestionAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsPollsManage,
            TagPolls
        );
    }
}
