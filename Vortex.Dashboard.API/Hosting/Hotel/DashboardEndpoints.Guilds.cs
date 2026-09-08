using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Api.Hotel;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Operations.Hotel;
using Vortex.Dashboard.API.Operations.Hotel.Contracts;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Guilds and their forums, read and written on one capability.
/// </summary>
/// <remarks>
/// The reads answer to the same capability as the writes rather than to <c>groups.read</c>, because
/// they show what nobody else may see: hidden threads, hidden posts and deleted posts, with their
/// text. That is the whole point of the page — an operator asked to explain a removal had nowhere to
/// look — and it is not something to hand to every holder of the analytics capability.
/// </remarks>
internal static partial class DashboardEndpoints
{
    private const string TagGuilds = "Guilds";
    private const string ApiGuilds = ApiV1 + "/guilds";
    private const string OpsGuilds = ApiOperations + "/guilds";

    public static void MapGuildReads(WebApplication app)
    {
        MapReadGet<GuildDirectoryPage>(
            app,
            ApiGuilds,
            (HttpContext ctx, GuildReads reads, CancellationToken ct) =>
                OkAsync(reads.GuildsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
        MapReadGetNullable<GuildModeration>(
            app,
            ApiGuilds + "/{guildId:int}",
            (int guildId, GuildReads reads, CancellationToken ct) =>
                OkNullableAsync(reads.GuildAsync(guildId, ct)),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
        MapReadGetNullable<GuildThreadDetail>(
            app,
            ApiGuilds + "/{guildId:int}/threads/{threadId:int}",
            (int guildId, int threadId, GuildReads reads, CancellationToken ct) =>
                OkNullableAsync(reads.ThreadAsync(guildId, threadId, ct)),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
    }

    public static void MapGuildOperations(WebApplication app)
    {
        MapPost(
            app,
            OpsGuilds + "/forum/thread",
            async (
                HttpContext ctx,
                ModerateForumThreadRequest body,
                GuildOperations ops,
                CancellationToken ct
            ) =>
                body.GuildId <= 0 || body.ThreadId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.ModerateThreadAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
        MapPost(
            app,
            OpsGuilds + "/forum/post",
            async (
                HttpContext ctx,
                ModerateForumPostRequest body,
                GuildOperations ops,
                CancellationToken ct
            ) =>
                body.GuildId <= 0 || body.PostId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.ModeratePostAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
        MapPost(
            app,
            OpsGuilds + "/member",
            async (
                HttpContext ctx,
                GuildMemberActionRequest body,
                GuildOperations ops,
                CancellationToken ct
            ) =>
                body.GuildId <= 0 || body.PlayerId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.MemberActionAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
        MapPost(
            app,
            OpsGuilds + "/delete",
            async (
                HttpContext ctx,
                DeleteGuildRequest body,
                GuildOperations ops,
                CancellationToken ct
            ) =>
                body.GuildId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteGuildAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGuildsManage,
            TagGuilds
        );
    }
}
