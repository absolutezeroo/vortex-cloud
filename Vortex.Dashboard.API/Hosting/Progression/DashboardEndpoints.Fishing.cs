using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Api.Progression;
using Vortex.Dashboard.API.Api.Progression.Contracts;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Operations.Progression;
using Vortex.Dashboard.API.Operations.Progression.Contracts;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Fishing content: the four tables whose every number is a guess reconstructed from Origins, which
/// is exactly why they are rows. Until this existed, retuning one still meant hand-written SQL.
/// Every write reloads the live definitions and pushes them to everyone currently fishing.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagFishing = "Fishing";
    private const string ApiFishing = ApiV1 + "/fishing";
    private const string OpsFishing = ApiOperations + "/fishing";

    public static void MapFishingReads(WebApplication app)
    {
        MapReadGet<FishingContent>(
            app,
            ApiFishing,
            (FishingReads reads, CancellationToken ct) => OkAsync(reads.FishingContentAsync(ct)),
            Capabilities.Dashboard.FishingRead,
            TagFishing
        );
        MapReadGet<FishingActivity>(
            app,
            ApiFishing + "/activity",
            (HttpContext ctx, FishingReads reads, CancellationToken ct) =>
                OkAsync(reads.FishingActivityAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FishingRead,
            TagFishing
        );
    }

    public static void MapFishingOperations(WebApplication app)
    {
        MapPost(
            app,
            OpsFishing + "/zones",
            async (
                HttpContext ctx,
                CreateFishingZoneRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.NameKey)
                || string.IsNullOrWhiteSpace(body.FurniClass)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateFishingZoneAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/zones/update",
            async (
                HttpContext ctx,
                UpdateFishingZoneRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.ZoneId <= 0 || string.IsNullOrWhiteSpace(body.NameKey)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateFishingZoneAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/zones/delete",
            async (
                HttpContext ctx,
                DeleteFishingZoneRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.ZoneId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteFishingZoneAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );

        MapPost(
            app,
            OpsFishing + "/species",
            async (
                HttpContext ctx,
                CreateFishingSpeciesRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.ZoneId <= 0 || string.IsNullOrWhiteSpace(body.NameKey)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateFishingSpeciesAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/species/update",
            async (
                HttpContext ctx,
                UpdateFishingSpeciesRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.SpeciesId <= 0 || body.ZoneId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateFishingSpeciesAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/species/delete",
            async (
                HttpContext ctx,
                DeleteFishingSpeciesRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.SpeciesId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteFishingSpeciesAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );

        MapPost(
            app,
            OpsFishing + "/rod-tiers",
            async (
                HttpContext ctx,
                CreateFishingRodTierRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.Quality <= 0 || string.IsNullOrWhiteSpace(body.NameKey)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateFishingRodTierAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/rod-tiers/update",
            async (
                HttpContext ctx,
                UpdateFishingRodTierRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.TierId <= 0 || body.Quality <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateFishingRodTierAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/rod-tiers/delete",
            async (
                HttpContext ctx,
                DeleteFishingRodTierRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.TierId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteFishingRodTierAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );

        MapPost(
            app,
            OpsFishing + "/levels",
            async (
                HttpContext ctx,
                CreateFishingLevelRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.Level <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateFishingLevelAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/levels/update",
            async (
                HttpContext ctx,
                UpdateFishingLevelRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.LevelId <= 0 || body.Level <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateFishingLevelAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
        MapPost(
            app,
            OpsFishing + "/levels/delete",
            async (
                HttpContext ctx,
                DeleteFishingLevelRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                body.LevelId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteFishingLevelAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );

        MapPost(
            app,
            OpsFishing + "/reload",
            async (
                HttpContext ctx,
                ReloadFishingRequest body,
                FishingOperations ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.ReloadFishingAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsFishingManage,
            TagFishing
        );
    }
}
