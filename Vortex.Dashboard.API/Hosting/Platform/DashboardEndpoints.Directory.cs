using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapDirectoryReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiDirectory + "/search",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.SearchAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiDirectory + "/players",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PlayersAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagDirectory
        );
        // Batch avatar-head resolver (?ids=1,2,3) so every surface that shows a player can render the
        // real head via lib/avatars.js. Users without PlayersRead simply keep the plain name.
        MapReadGet(
            app,
            ApiDirectory + "/avatars",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.AvatarsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagDirectory
        );
        // Room search for the shared picker, so a surface that pins something to a room hands back
        // an id the operator picked rather than typed.
        MapReadGet(
            app,
            ApiDirectory + "/rooms",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.RoomsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/furniture",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.FurnitureDefinitionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FurnitureRead,
            TagDirectory
        );
        // The directories behind the reward-track filter pickers. Each replaces an id or a code an
        // operator was expected to type from memory into a filter that saves cleanly and then never
        // matches -- the failure this subsystem exists to prevent, reappearing at the last step.
        MapReadGet(
            app,
            ApiDirectory + "/groups",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.GroupsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.SocialRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/habbicons",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.HabbiconsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.HabbiconsRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/habbicon-collections",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.HabbiconCollectionsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.HabbiconsRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/catalog-offers",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.CatalogOffersDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CatalogRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/navigator-categories",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.NavigatorCategoriesDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.NavigatorRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/badges",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.BadgesDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/pet-species",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PetSpeciesDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PetsRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/polls",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PollsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PollsRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/quizzes",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.QuizzesDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.QuestsRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/quest-campaigns",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.QuestCampaignsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.QuestsRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/vouchers",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.VouchersDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/club-gifts",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ClubGiftsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CatalogRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/nft-store",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.NftStoreDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CollectiblesRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/targeted-offers",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.TargetedOffersDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CatalogRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/forum-threads",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ForumThreadsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.SocialRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/avatar-effects",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.AvatarEffectsDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/placed-furniture",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PlacedFurnitureDirectoryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FurnitureRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/entity/{id}",
            (string id, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.ItemAsync(id, ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiDirectory + "/rooms/{roomId:int}",
            (int roomId, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.RoomTimelineAsync(roomId, ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
    }
}
