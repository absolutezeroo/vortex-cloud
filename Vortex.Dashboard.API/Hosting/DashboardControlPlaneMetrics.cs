using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
///     Counts what the dashboard's request pipeline refuses.
/// </summary>
/// <remarks>
///     <para>
///     Registered above <c>UseAuthentication</c>/<c>UseAuthorization</c> rather than beside the access
///     audit at the bottom of the pipeline, and that placement is the whole point: those two
///     short-circuit. A 401 or a 403 never reaches an endpoint, so nothing registered below them ever
///     sees one — and the refusals are exactly the half worth counting. An operator walking routes
///     they have no capability for produces no other signal at all.
///     </para>
///     <para>
///     Its own file so it can be exercised on a bare test server: the rule it applies (which statuses
///     count, which paths, and what a 403 is tagged with) is worth a test, and the host it normally
///     lives in needs Orleans and a database to build.
///     </para>
/// </remarks>
internal static class DashboardControlPlaneMetrics
{
    /// <summary>Returned when an endpoint required a signed-in operator but no named capability.</summary>
    internal const string NO_CAPABILITY = "none";

    internal static void Use(IApplicationBuilder app, IVortexMetrics metrics) =>
        app.Use(
            async (ctx, next) =>
            {
                await next().ConfigureAwait(false);

                // The SPA's own assets fail for reasons that are not control-plane events; the API
                // surface is what an operator is held to. Same rule the access audit applies.
                if (!(ctx.Request.Path.Value ?? "/").StartsWith("/api/", StringComparison.Ordinal))
                {
                    return;
                }

                int status = ctx.Response.StatusCode;

                if (status < StatusCodes.Status400BadRequest)
                {
                    return;
                }

                metrics.DashboardHttpError(status);

                if (status == StatusCodes.Status403Forbidden)
                {
                    metrics.DashboardAuthorizationDenied(RequiredCapability(ctx));
                }
            }
        );

    /// <summary>
    ///     The capability an endpoint asked for, or <see cref="NO_CAPABILITY" /> when it only required a
    ///     signed-in operator.
    /// </summary>
    /// <remarks>
    ///     The policy name <em>is</em> the capability — <c>RequireAuthorization(capability)</c> is how
    ///     every guarded route declares itself, and the policies are built from
    ///     <c>Capabilities.Dashboard.All</c> — so the tag stays inside a closed set and cannot grow a
    ///     time series per request. Reading it off the response instead would give nothing: a 403 body
    ///     does not say which capability was missing, on purpose.
    /// </remarks>
    internal static string RequiredCapability(HttpContext ctx)
    {
        Microsoft.AspNetCore.Http.Endpoint? endpoint = ctx.GetEndpoint();

        if (endpoint is null)
        {
            return NO_CAPABILITY;
        }

        string? policy = null;

        // Last named policy wins: an endpoint can carry several authorization attributes (a group's
        // and its own), and the one RequireAuthorization(capability) attached is the innermost.
        foreach (IAuthorizeData authorize in endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>())
        {
            if (!string.IsNullOrEmpty(authorize.Policy))
            {
                policy = authorize.Policy;
            }
        }

        return policy ?? NO_CAPABILITY;
    }
}
