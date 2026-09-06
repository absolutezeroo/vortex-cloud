using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Infrastructure;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Serves the hotel's own asset pack — furni icons, badges, promo art — off this listener.
/// </summary>
/// <remarks>
/// The image templates used to name the asset host directly
/// (<c>http://vortex-assets.local/c_images/…</c>), which works on the machine that has the vhost and
/// nowhere else: every picture in the dashboard is broken from a phone, from a colleague's laptop, or
/// through a tunnel. Reading the files from <c>AssetsLocalRoot</c> and serving them here makes the
/// dashboard self-contained, and the templates become relative paths that resolve wherever it is
/// reached from.
/// <para>
/// Files, not an HTTP proxy: the path is already configured for the image picker, and going through
/// the asset host would put a second hop and a second failure mode behind every icon.
/// </para>
/// </remarks>
internal static partial class DashboardEndpoints
{
    /// <summary>
    /// The top-level folders of the asset pack a dashboard page has any business reading. A closed
    /// list: the rest of that tree is the client's business, and the segment comes off a URL.
    /// </summary>
    private static readonly string[] HotelAssetRoots = ["c_images", "dcr", "gamedata"];

    private static readonly FrozenDictionary<string, string> HotelAssetContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp",
            [".svg"] = "image/svg+xml",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static void MapHotelAssets(WebApplication app)
    {
        app.MapGet(
                "/hotel-assets/{**path}",
                (string path, DashboardAssetUrls assets) =>
                {
                    if (string.IsNullOrWhiteSpace(assets.LocalRoot))
                    {
                        return Results.NotFound();
                    }

                    if (!TryResolveHotelAsset(assets.LocalRoot, path, out string resolved))
                    {
                        return Results.NotFound();
                    }

                    string extension = Path.GetExtension(resolved);

                    if (
                        !HotelAssetContentTypes.TryGetValue(extension, out string? contentType)
                        || !File.Exists(resolved)
                    )
                    {
                        return Results.NotFound();
                    }

                    // Content-hashed by name in practice (icon_1234.png never changes meaning), and
                    // an operator opening the furniture list should not re-fetch 200 icons.
                    return Results.File(resolved, contentType, enableRangeProcessing: false);
                }
            )
            // Anonymous like the SPA's own assets: these are the same public pictures the game client
            // downloads, and gating them would mean the login screen cannot draw itself.
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    /// <summary>
    /// Turns a request path into a real file under the asset root, or refuses.
    /// </summary>
    /// <remarks>
    /// The whole point of this method is that <paramref name="path"/> comes from the network. It must
    /// start with one of the allowed folders and, after the operating system has had its say about
    /// <c>..</c> and symlinks, still sit under the root — a check on the raw string alone is the
    /// traversal bug, not the fix for it.
    /// </remarks>
    private static bool TryResolveHotelAsset(string root, string path, out string resolved)
    {
        resolved = string.Empty;

        if (string.IsNullOrWhiteSpace(path) || path.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        int slash = path.IndexOf('/', StringComparison.Ordinal);
        string folder = slash < 0 ? path : path[..slash];

        if (Array.IndexOf(HotelAssetRoots, folder) < 0)
        {
            return false;
        }

        string rootFull = Path.GetFullPath(root);
        string candidate = Path.GetFullPath(
            Path.Combine(rootFull, path.Replace('/', Path.DirectorySeparatorChar))
        );

        if (!candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        resolved = candidate;
        return true;
    }
}
