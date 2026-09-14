using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api.Hotel;

/// <summary>
/// The hotel view, read for the page that configures it.
/// </summary>
/// <remarks>
/// No database is involved and there is none to involve: the hotel view is the
/// <c>landing.view.*</c> block of external_variables.json, which the game client downloads at boot.
/// Nothing here is server state, so there is no row that could disagree with the file.
/// <para>
/// external_flash_texts is read alongside it because a promo's wiring and a promo's words are in
/// different files, and showing an operator <c>landing.view.jan26cf.header</c> where the headline
/// should be is showing them the plumbing instead of the page.
/// </para>
/// <para>
/// It still derives <see cref="DashboardReads"/> without ever opening a context, because that base is
/// how the web host <i>recognises</i> a subject's read class and forwards it into the endpoint
/// container. A read class that does not derive it is not merely unforwarded: minimal APIs read an
/// unresolvable parameter as a request body and the whole dashboard fails to start.
/// </para>
/// </remarks>
internal sealed class HotelViewReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    GamedataDocumentStore gamedata,
    DashboardAssetUrls assets
) : DashboardReads(dbContextFactory)
{
    private readonly GamedataDocumentStore _gamedata = gamedata;
    private readonly DashboardAssetUrls _assets = assets;

    public HotelViewConfig HotelView()
    {
        if (!_gamedata.Available)
        {
            return Unavailable();
        }

        JsonNode? variables = _gamedata.Read("variables", null, out DateTime modified);

        if (variables is not JsonObject map)
        {
            return Unavailable();
        }

        JsonNode? texts = _gamedata.Read("texts", null, out DateTime textsModified);

        return LandingViewDocument.Read(
            map,
            texts as JsonObject,
            modified,
            textsModified,
            AssetFallbacks()
        );
    }

    /// <summary>
    /// The <c>${…}</c> tokens no gamedata file can answer.
    /// </summary>
    /// <remarks>
    /// <c>url.prefix</c> is the hotel's own asset origin, which the client substitutes at boot from
    /// its configuration rather than reading from a file — so it appears in every stored image URL
    /// and in none of the dumps. Taken from the dashboard's asset base, which is the same host by
    /// construction, and left out when none is configured: a preview that cannot resolve shows the
    /// operator the path they typed, which is better than a URL pointing at the dashboard itself.
    /// </remarks>
    private Dictionary<string, string> AssetFallbacks()
    {
        string? images = _assets.ArticleImageBase;

        return images is null
            ? []
            : new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["url.prefix"] = images[..^"/c_images".Length],
            };
    }

    /// <summary>
    /// What the page gets when the asset root is not configured or the file will not parse.
    /// </summary>
    /// <remarks>
    /// The vocabulary travels even here. The page's whole value is explaining what the keys mean, and
    /// an operator who cannot reach the file is exactly the one who wants to read that.
    /// </remarks>
    private static HotelViewConfig Unavailable() =>
        new(
            false,
            null,
            null,
            new HotelViewCommon(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                []
            ),
            [],
            [],
            [],
            [],
            new Dictionary<string, string>(),
            LandingViewVocabulary.Describe()
        );
}
