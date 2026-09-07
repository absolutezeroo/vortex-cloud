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
/// Habbicon and reward-track admin surface: content CRUD, the track lifecycle (clone, publish,
/// archive), and the two per-player operations an operator needs.
/// </summary>
/// <remarks>
/// Every write goes through <c>RewardOperations</c>, which routes to the domain's own
/// admin service — those reload the in-process catalogs and, for reward tracks, tell the players
/// who already have progress that the content changed. Nothing here writes to the database.
/// </remarks>
internal static partial class DashboardEndpoints
{
    private const string TagHabbicons = "Habbicons";
    private const string TagRewardTracks = "Reward tracks";
    private const string ApiHabbicons = ApiV1 + "/habbicons";
    private const string ApiRewardTracks = ApiV1 + "/reward-tracks";

    public static void MapHabbiconReads(WebApplication app)
    {
        MapReadGet<HabbiconCollectionList>(
            app,
            ApiHabbicons,
            (HttpContext ctx, HabbiconReads habbicons, CancellationToken ct) =>
                OkAsync(habbicons.HabbiconCollectionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.HabbiconsRead,
            TagHabbicons
        );
        MapReadGet<HabbiconSourceOptions>(
            app,
            ApiHabbicons + "/sources",
            (HabbiconReads habbicons) => Results.Ok(habbicons.HabbiconSourceOptions()),
            Capabilities.Dashboard.HabbiconsRead,
            TagHabbicons
        );
        MapReadGet<PlayerHabbicons>(
            app,
            ApiHabbicons + "/players/{playerId:int}",
            (int playerId, HabbiconReads habbicons, CancellationToken ct) =>
                OkAsync(habbicons.PlayerHabbiconsAsync(playerId, ct)),
            Capabilities.Dashboard.HabbiconsRead,
            TagHabbicons
        );
    }

    public static void MapRewardTrackReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiRewardTracks,
            (HttpContext ctx, RewardTrackReads tracks, CancellationToken ct) =>
                OkAsync(tracks.RewardTracksAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.RewardTracksRead,
            TagRewardTracks
        );
        MapReadGet(
            app,
            ApiRewardTracks + "/actions",
            (RewardTrackReads tracks) => Results.Ok(tracks.RewardTrackActionOptions()),
            Capabilities.Dashboard.RewardTracksRead,
            TagRewardTracks
        );
        MapReadGet(
            app,
            ApiRewardTracks + "/reward-kinds",
            (RewardTrackReads tracks) => Results.Ok(tracks.RewardTrackRewardKindOptions()),
            Capabilities.Dashboard.RewardTracksRead,
            TagRewardTracks
        );
        MapReadGet(
            app,
            ApiRewardTracks + "/players/{playerId:int}",
            (int playerId, RewardTrackReads tracks, CancellationToken ct) =>
                OkAsync(tracks.PlayerRewardTracksAsync(playerId, ct)),
            Capabilities.Dashboard.RewardTracksRead,
            TagRewardTracks
        );
    }

    public static void MapHabbiconOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/habbicons/collections",
            async (
                HttpContext ctx,
                CreateHabbiconCollectionRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateHabbiconCollectionAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons/collections/update",
            async (
                HttpContext ctx,
                UpdateHabbiconCollectionRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.CollectionId <= 0 || string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateHabbiconCollectionAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons/collections/delete",
            async (
                HttpContext ctx,
                DeleteHabbiconCollectionRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.CollectionId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteHabbiconCollectionAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons",
            async (
                HttpContext ctx,
                CreateHabbiconRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code) || body.CollectionId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateHabbiconAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons/update",
            async (
                HttpContext ctx,
                UpdateHabbiconRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.HabbiconId <= 0
                || string.IsNullOrWhiteSpace(body.Code)
                || body.CollectionId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateHabbiconAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons/delete",
            async (
                HttpContext ctx,
                DeleteHabbiconRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.HabbiconId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteHabbiconAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons/grant",
            async (
                HttpContext ctx,
                GrantHabbiconRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.PlayerId <= 0 || body.HabbiconId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.GrantHabbiconAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
        MapPost(
            app,
            ApiOperations + "/habbicons/revoke",
            async (
                HttpContext ctx,
                RevokeHabbiconRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.PlayerId <= 0 || body.HabbiconId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.RevokeHabbiconAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsHabbiconsManage,
            TagHabbicons
        );
    }

    public static void MapRewardTrackOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/reward-tracks",
            async (
                HttpContext ctx,
                CreateRewardTrackRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.TrackId)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/update",
            async (
                HttpContext ctx,
                UpdateRewardTrackRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0 || string.IsNullOrWhiteSpace(body.TrackId)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/clone",
            async (
                HttpContext ctx,
                CloneRewardTrackRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0 || string.IsNullOrWhiteSpace(body.NewTrackId)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CloneRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/publish",
            async (
                HttpContext ctx,
                RewardTrackRowRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.PublishRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/archive",
            async (
                HttpContext ctx,
                RewardTrackRowRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.ArchiveRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/delete",
            async (
                HttpContext ctx,
                RewardTrackRowRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/tasks",
            async (
                HttpContext ctx,
                UpsertRewardTrackTaskRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0
                || string.IsNullOrWhiteSpace(body.TaskId)
                || string.IsNullOrWhiteSpace(body.ActionCode)
                || body.Levels is null
                || body.Levels.Count == 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpsertRewardTrackTaskAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/tasks/delete",
            async (
                HttpContext ctx,
                DeleteRewardTrackTaskRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TaskRowId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteRewardTrackTaskAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/prizes",
            async (
                HttpContext ctx,
                UpsertRewardTrackPrizeRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.TrackRowId <= 0
                || string.IsNullOrWhiteSpace(body.PrizeId)
                || body.Rewards is null
                || body.Rewards.Count == 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpsertRewardTrackPrizeAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/prizes/delete",
            async (
                HttpContext ctx,
                DeleteRewardTrackPrizeRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.PrizeRowId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteRewardTrackPrizeAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/players/reset",
            async (
                HttpContext ctx,
                ResetPlayerRewardTrackRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.PlayerId <= 0 || string.IsNullOrWhiteSpace(body.TrackId)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.ResetPlayerRewardTrackAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
        MapPost(
            app,
            ApiOperations + "/reward-tracks/players/grant-premium",
            async (
                HttpContext ctx,
                GrantRewardTrackPremiumRequest body,
                RewardOperations ops,
                CancellationToken ct
            ) =>
                body.PlayerId <= 0 || string.IsNullOrWhiteSpace(body.TrackId)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.GrantRewardTrackPremiumAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsRewardTracksManage,
            TagRewardTracks
        );
    }
}
