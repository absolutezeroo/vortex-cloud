using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Community goals and daily tasks: the content the quest system runs on that is not itself a quest.
/// Deliberately under the existing quest capability rather than a new one — it is the same domain
/// and the same operators, and a capability nobody has been granted is a page nobody can open.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagQuestContent = "Quests";
    private const string ApiCommunityGoals = ApiV1 + "/community-goals";
    private const string ApiDailyTasks = ApiV1 + "/daily-tasks";

    public static void MapQuestContentReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiCommunityGoals,
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.CommunityGoalsAsync(ct)),
            Capabilities.Dashboard.QuestsRead,
            TagQuestContent
        );
        MapReadGet(
            app,
            ApiDailyTasks,
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.DailyTasksAsync(ct)),
            Capabilities.Dashboard.QuestsRead,
            TagQuestContent
        );
    }

    public static void MapQuestContentOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/community-goals",
            async (
                HttpContext ctx,
                CreateCommunityGoalRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateCommunityGoalAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuestContent
        );
        MapPost(
            app,
            ApiOperations + "/community-goals/update",
            async (
                HttpContext ctx,
                UpdateCommunityGoalRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.GoalId <= 0 || string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateCommunityGoalAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuestContent
        );
        MapPost(
            app,
            ApiOperations + "/community-goals/delete",
            async (
                HttpContext ctx,
                DeleteCommunityGoalRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.GoalId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteCommunityGoalAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuestContent
        );
        MapPost(
            app,
            ApiOperations + "/daily-tasks",
            async (
                HttpContext ctx,
                CreateDailyTaskRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.TaskCode)
                || string.IsNullOrWhiteSpace(body.QuestTypeCode)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateDailyTaskAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuestContent
        );
        MapPost(
            app,
            ApiOperations + "/daily-tasks/update",
            async (
                HttpContext ctx,
                UpdateDailyTaskRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.TaskId <= 0
                || string.IsNullOrWhiteSpace(body.TaskCode)
                || string.IsNullOrWhiteSpace(body.QuestTypeCode)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateDailyTaskAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuestContent
        );
        MapPost(
            app,
            ApiOperations + "/daily-tasks/delete",
            async (
                HttpContext ctx,
                DeleteDailyTaskRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.TaskId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteDailyTaskAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuestContent
        );
    }
}
