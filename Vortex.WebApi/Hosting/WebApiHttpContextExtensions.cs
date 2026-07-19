using Microsoft.AspNetCore.Http;
using Vortex.WebApi.Session;

namespace Vortex.WebApi.Hosting;

/// <summary>
/// Adapts ASP.NET Core request/response state onto the web API's cookie-backed session model,
/// replacing the manual <c>HttpListener</c> cookie parsing. The session cookie name is preserved so
/// the existing Flash onboarding client keeps working unchanged.
/// </summary>
internal static class WebApiHttpContextExtensions
{
    public const string SessionCookieName = "habbo-web-session";

    /// <summary>The opaque session id carried by the request cookie, or null when absent.</summary>
    public static string? SessionId(this HttpContext ctx)
    {
        string? value = ctx.Request.Cookies[SessionCookieName];

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>The authenticated account id for the current session, or null when unauthenticated.</summary>
    public static int? AccountId(this HttpContext ctx, WebApiSessionStore sessions) =>
        sessions.GetAccountId(ctx.SessionId());

    /// <summary>The caller's remote IP, defaulting to loopback when it cannot be resolved.</summary>
    public static string RemoteIp(this HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

    /// <summary>Issues the session cookie using the same attributes the listener emitted (HttpOnly, Lax).</summary>
    public static void IssueSessionCookie(this HttpContext ctx, string sessionId) =>
        ctx.Response.Cookies.Append(
            SessionCookieName,
            sessionId,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                Secure = ctx.Request.IsHttps,
            }
        );
}
