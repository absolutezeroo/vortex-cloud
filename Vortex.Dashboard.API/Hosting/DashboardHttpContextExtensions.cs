using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Vortex.Dashboard.API.Security;

namespace Vortex.Dashboard.API.Hosting;

internal static class DashboardHttpContextExtensions
{
    /// <summary>The rich principal stashed by <see cref="DashboardAuthenticationHandler" />, or null.</summary>
    public static DashboardPrincipal? GetDashboardPrincipal(this HttpContext ctx)
    {
        return ctx.Items.TryGetValue(
            DashboardAuthenticationHandler.PrincipalItemKey,
            out object? value
        )
            ? value as DashboardPrincipal
            : null;
    }

    /// <summary>The authenticated operator's email for audit attribution, or "anonymous".</summary>
    public static string ActorEmail(this HttpContext ctx)
    {
        return ctx.GetDashboardPrincipal()?.Email ?? "anonymous";
    }

    /// <summary>
    ///     Whether the authenticated operator holds <paramref name="capability"/>. Used where the
    ///     capability is only known at request time — a console command names its own — and so
    ///     cannot be declared as a static authorization policy on the endpoint.
    /// </summary>
    public static bool HoldsCapability(this HttpContext ctx, string capability)
    {
        return ctx.GetDashboardPrincipal()?.Has(capability) ?? false;
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
