using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Quest admin surface: read (list/detail), completion analytics (stats), and the CRUD operations
/// that edit a quest live — every write reloads the in-memory quest cache so changes (reward, timer,
/// steps…) take effect without an emulator restart (see <c>IQuestAdminService</c>).
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagQuests = "Quests";
    private const string ApiQuests = ApiV1 + "/quests";

    public static void MapQuestReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiQuests,
            "/api/quests",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.QuestsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.QuestsRead,
            TagQuests
        );
        MapReadGet(
            app,
            ApiQuests + "/stats",
            "/api/quests/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.QuestsStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.QuestsRead,
            TagQuests
        );
        MapReadGet(
            app,
            ApiQuests + "/types",
            "/api/quests/types",
            (DashboardApiService api) => Results.Ok(api.QuestTypeOptions()),
            Capabilities.Dashboard.QuestsRead,
            TagQuests
        );
        MapReadGet(
            app,
            ApiQuests + "/{questId:int}",
            "/api/quests/{questId:int}",
            (int questId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.QuestDetailAsync(questId, ct)),
            Capabilities.Dashboard.QuestsRead,
            TagQuests
        );
    }

    public static void MapQuestOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/quests",
            "/api/operations/quests",
            async (
                HttpContext ctx,
                CreateQuestRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || string.IsNullOrWhiteSpace(body.CampaignCode)
                    || string.IsNullOrWhiteSpace(body.LocalizationCode)
                    || string.IsNullOrWhiteSpace(body.QuestType)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateQuestAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuests
        );
        MapPost(
            app,
            ApiOperations + "/quests/update",
            "/api/operations/quests/update",
            async (
                HttpContext ctx,
                UpdateQuestRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.QuestId <= 0
                    || string.IsNullOrWhiteSpace(body.CampaignCode)
                    || string.IsNullOrWhiteSpace(body.LocalizationCode)
                    || string.IsNullOrWhiteSpace(body.QuestType)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateQuestAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuests
        );
        MapPost(
            app,
            ApiOperations + "/quests/delete",
            "/api/operations/quests/delete",
            async (
                HttpContext ctx,
                DeleteQuestRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.QuestId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteQuestAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsQuestsManage,
            TagQuests
        );
    }
}
