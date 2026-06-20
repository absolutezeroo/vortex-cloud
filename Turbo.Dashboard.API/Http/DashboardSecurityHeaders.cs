using System;
using Microsoft.AspNetCore.Http;

namespace Turbo.Dashboard.API.Http;

/// <summary>
/// Builds and applies the dashboard's hardened response headers (CSP + the usual security headers).
/// The CSP locks scripts/styles to <c>'self'</c>, so it is applied to the SPA and JSON API but not to
/// the Swagger UI (which ships its own inline bootstrap script) — see the host pipeline.
/// </summary>
internal static class DashboardSecurityHeaders
{
    /// <summary>
    /// Builds the Content-Security-Policy. When a furniture icon host is configured, its origin is
    /// added to <c>img-src</c> so operator tools can render furni icons.
    /// </summary>
    public static string BuildCsp(string? furniIconTemplate)
    {
        string imgSrc = "img-src 'self' data:";
        string? origin = TryGetIconOrigin(furniIconTemplate);

        if (origin is not null)
        {
            imgSrc += " " + origin;
        }

        return "default-src 'self'; script-src 'self'; style-src 'self'; "
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

    private static string? TryGetIconOrigin(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        string probe = template.Replace("{name}", "icon", StringComparison.Ordinal);

        return
            Uri.TryCreate(probe, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
    }
}
