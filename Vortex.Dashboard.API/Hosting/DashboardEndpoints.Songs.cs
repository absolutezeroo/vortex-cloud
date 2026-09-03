using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// The song catalogue: the table a jukebox plays out of, and until this existed it could only be
/// filled by hand in SQL. Every write reloads the live catalogue, so a song is playable the moment
/// it is saved.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagSongs = "Songs";
    private const string ApiSongs = ApiV1 + "/songs";

    public static void MapSongReads(WebApplication app) =>
        MapReadGet(
            app,
            ApiSongs,
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.SongsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.SongsRead,
            TagSongs
        );

    public static void MapSongOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/songs",
            async (
                HttpContext ctx,
                CreateSongRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Name) || body.LengthSeconds <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateSongAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsSongsManage,
            TagSongs
        );
        MapPost(
            app,
            ApiOperations + "/songs/update",
            async (
                HttpContext ctx,
                UpdateSongRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.SongId <= 0 || string.IsNullOrWhiteSpace(body.Name) || body.LengthSeconds <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateSongAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsSongsManage,
            TagSongs
        );
        MapPost(
            app,
            ApiOperations + "/songs/delete",
            async (
                HttpContext ctx,
                DeleteSongRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.SongId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteSongAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsSongsManage,
            TagSongs
        );
        MapPost(
            app,
            ApiOperations + "/songs/reload",
            async (
                HttpContext ctx,
                ReloadSongsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.ReloadSongsAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsSongsManage,
            TagSongs
        );
    }
}
