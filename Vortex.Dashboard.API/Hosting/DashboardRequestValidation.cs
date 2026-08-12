using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Marks an operation request body that carries the mandatory audited reason. Implemented by every
/// dashboard write request; <see cref="DashboardRequestValidationFilter"/> enforces it once for all
/// of them.
/// </summary>
internal interface IReasonedRequest
{
    string Reason { get; }
}

/// <summary>
/// The two checks every dashboard write endpoint was making by hand: that a body arrived at all, and
/// that it carries a usable reason. Both were copied into all 47 write endpoints as the opening
/// clauses of a bespoke <c>if</c>, which meant a new endpoint could silently ship without either —
/// and one written without the reason clause would accept an unjustified write and audit it with an
/// empty string.
///
/// Endpoint-specific field rules deliberately stay in the endpoint: they are what that endpoint's
/// contract actually is, and reading them next to the route is the point. This filter only removes
/// the part that was identical everywhere.
/// </summary>
internal sealed class DashboardRequestValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        foreach (object? argument in context.Arguments)
        {
            // Every argument these handlers take is either the framework's (HttpContext, a resolved
            // service, the CancellationToken) or the request body, and the framework never hands over
            // a null for its own. A null here therefore means the JSON body was absent or literally
            // `null`, which the handler would only dereference.
            if (argument is null)
            {
                return InvalidRequest;
            }

            if (argument is IReasonedRequest reasoned && !HasReason(reasoned.Reason))
            {
                return InvalidRequest;
            }
        }

        return await next(context).ConfigureAwait(false);
    }

    private static IResult InvalidRequest => Results.BadRequest(new { error = "invalid_request" });

    /// <summary>
    /// A reason has to be something an operator can be held to later. Three characters is the same
    /// floor the dashboard's own <c>reasonOk</c> applies before it will open the confirm dialog, so
    /// the client and the server agree on what counts.
    /// </summary>
    internal static bool HasReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 3;
}
