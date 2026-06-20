using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Turbo.Dashboard.API.Security;

namespace Turbo.Dashboard.API.Hosting;

internal static class DashboardHttpContextExtensions
{
    /// <summary>The rich principal stashed by <see cref="DashboardAuthenticationHandler" />, or null.</summary>
    public static DashboardPrincipal? GetDashboardPrincipal(this HttpContext ctx)
    {
        return ctx.Items.TryGetValue(DashboardAuthenticationHandler.PrincipalItemKey, out object? value)
            ? value as DashboardPrincipal
            : null;
    }

    /// <summary>The authenticated operator's email for audit attribution, or "anonymous".</summary>
    public static string ActorEmail(this HttpContext ctx)
    {
        return ctx.GetDashboardPrincipal()?.Email ?? "anonymous";
    }

    /// <summary>
    ///     Adapts the request query into the <see cref="NameValueCollection" /> shape the existing
    ///     <c>DashboardApiService</c> read methods consume, so those methods stay untouched.
    /// </summary>
    public static NameValueCollection QueryAsNameValues(this HttpContext ctx)
    {
        NameValueCollection result = new();

        foreach (KeyValuePair<string, StringValues> pair in ctx.Request.Query)
        {
            foreach (string? value in pair.Value)
            {
                result.Add(pair.Key, value);
            }
        }

        return result;
    }
}
