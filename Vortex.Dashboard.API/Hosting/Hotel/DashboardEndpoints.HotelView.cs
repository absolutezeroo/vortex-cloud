using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api.Hotel;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Operations.Hotel;
using Vortex.Dashboard.API.Operations.Hotel.Contracts;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// The hotel view -- the first screen a player sees -- read and written from the dashboard.
/// </summary>
/// <remarks>
/// Under the gamedata capability rather than one of its own. This page edits exactly the file the
/// gamedata page edits, with the same blast radius; a second capability would be a second thing to
/// grant that protects nothing, and the one it protects against is already granted.
/// </remarks>
internal static partial class DashboardEndpoints
{
    private const string TagHotelView = "HotelView";

    public static void MapHotelViewReads(WebApplication app) =>
        MapReadGet<HotelViewConfig>(
            app,
            ApiV1 + "/hotelview",
            (HotelViewReads reads) => Results.Ok(reads.HotelView()),
            Capabilities.Dashboard.OpsGamedataManage,
            TagHotelView
        );

    public static void MapHotelViewOperations(WebApplication app) =>
        MapPost(
            app,
            ApiOperations + "/hotelview",
            async (
                HttpContext ctx,
                HotelViewSaveRequest body,
                HotelViewOperations ops,
                CancellationToken ct
            ) =>
                body.Slots is null || body.Common is null
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SaveHotelViewAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGamedataManage,
            TagHotelView
        );
}
