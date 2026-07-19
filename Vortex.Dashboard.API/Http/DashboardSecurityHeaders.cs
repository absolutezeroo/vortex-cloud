using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Vortex.Dashboard.API.Http;

/// <summary>
/// Builds and applies the dashboard's hardened response headers (CSP + the usual security headers).
/// The CSP locks scripts/styles to <c>'self'</c>, so it is applied to the SPA and JSON API but not to
/// the Swagger UI (which ships its own inline bootstrap script) — see the host pipeline.
/// </summary>
internal static class DashboardSecurityHeaders
{
    /// <summary>
    /// Builds the Content-Security-Policy. Each configured asset host origin (furniture/catalog icons,
    /// targeted-offer images, avatar heads, guild badges — resolved once by <c>DashboardAssetUrls</c>)
    /// is added to <c>img-src</c> so operator tools can render those images.
    /// </summary>
    public static string BuildCsp(IEnumerable<string>? imgOrigins)
    {
        string imgSrc = "img-src 'self' data:";

        foreach (string origin in (imgOrigins ?? []).Distinct(StringComparer.Ordinal))
        {
            imgSrc += " " + origin;
        }

        // style-src allows 'unsafe-inline': the SPA is fully client-rendered (Svelte creates every
        // element via JS, there is no server-rendered HTML), so even a static-looking `style="..."`
        // in a component's markup compiles to a runtime `element.style` mutation -- CSP treats that
        // identically to a dynamically computed one. Dozens of components across the dashboard rely
        // on this (chart positioning, spacing utilities, severity-colored borders), so this is a
        // blanket, deliberate relaxation, not a per-component patch. script-src stays locked to
        // 'self' -- that is the directive that actually matters for XSS defense; inline *style*
        // injection has a much smaller blast radius (CSS-only, no code execution) and this dashboard
        // renders no user-generated content as literal HTML anywhere.
        return "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; "
            + imgSrc
            + "; connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'none';";
    }

    public static void Apply(HttpResponse response, string csp)
    {
        IHeaderDictionary headers = response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["X-Frame-Options"] = "DENY";
        headers["X-XSS-Protection"] = "0";
        headers["Content-Security-Policy"] = csp;
        headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        headers["Pragma"] = "no-cache";
        headers["Expires"] = "0";
    }
}
