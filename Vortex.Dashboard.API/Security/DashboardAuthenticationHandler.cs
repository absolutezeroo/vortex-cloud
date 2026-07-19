using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vortex.Dashboard.API.Security;

/// <summary>
///     Bridges the dashboard's cookie + in-memory session model into ASP.NET Core authentication so the
///     minimal-API endpoints can rely on standard <c>[Authorize]</c>/authorization policies (and so the
///     scheme is documented in Swagger). The <c>dash_session</c> cookie is resolved to a
///     <see cref="DashboardPrincipal" /> on every request — capabilities are therefore re-checked each
///     time, so revoking a role takes effect immediately. The resolved principal is stashed in
///     <c>HttpContext.Items</c> for endpoints that need the actor email or account id.
/// </summary>
internal sealed class DashboardAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DashboardAuthService auth
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DashboardSession";

    public const string SessionCookieName = "dash_session";

    public const string CapabilityClaimType = "dashboard:cap";

    public const string PrincipalItemKey = "dashboard:principal";

    private readonly DashboardAuthService _auth = auth;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? sessionId = Request.Cookies[SessionCookieName];

        if (string.IsNullOrEmpty(sessionId))
        {
            return AuthenticateResult.NoResult();
        }

        DashboardPrincipal? principal = await _auth
            .ResolveAsync(sessionId, Context.RequestAborted)
            .ConfigureAwait(false);

        if (principal is null)
        {
            return AuthenticateResult.NoResult();
        }

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Name, principal.Email),
            new Claim(ClaimTypes.NameIdentifier, principal.AccountId.ToString()),
        };

        foreach (string capability in principal.Permissions.Capabilities)
        {
            claims.Add(new Claim(CapabilityClaimType, capability));
        }

        ClaimsIdentity identity = new(claims, SchemeName);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), SchemeName);

        // Endpoints read the rich principal (capability checks + actor email) from here.
        Context.Items[PrincipalItemKey] = principal;

        return AuthenticateResult.Success(ticket);
    }
}
