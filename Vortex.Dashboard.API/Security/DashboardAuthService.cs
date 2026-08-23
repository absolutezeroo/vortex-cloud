using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Security;

/// <summary>
/// Authenticates dashboard operators against the account system and resolves their capabilities.
/// Access requires both valid credentials and at least one <c>dashboard.*</c> capability, so a plain
/// player account cannot open the dashboard even with correct credentials.
/// </summary>
internal sealed class DashboardAuthService(
    IAccountAuthenticator authenticator,
    IPermissionService permissions,
    DashboardSessionStore sessions
)
{
    public async Task<DashboardLoginResult> LoginAsync(
        string email,
        string password,
        string? code,
        CancellationToken ct
    )
    {
        // The second factor is the authenticator's business now, not this method's -- so the web API
        // login enforces the same factor without anyone having remembered to copy this block.
        AccountVerification verification = await authenticator
            .VerifyCredentialsAsync(email, password, code, ct)
            .ConfigureAwait(false);

        switch (verification.Outcome)
        {
            case AccountVerificationOutcome.MfaRequired:
                return DashboardLoginResult.MfaRequired;
            case AccountVerificationOutcome.InvalidCode:
                return DashboardLoginResult.InvalidCode;
            case AccountVerificationOutcome.InvalidCredentials:
                return DashboardLoginResult.InvalidCredentials;
        }

        PermissionSet perms = await permissions
            .ResolveForAccountAsync(verification.AccountId, ct)
            .ConfigureAwait(false);

        // Authorization now runs after the factor rather than before it. That is the right order:
        // "you have no dashboard access" is something to say to someone who has finished proving who
        // they are, not to someone holding half a credential.
        if (!HasDashboardAccess(perms))
        {
            return DashboardLoginResult.Forbidden;
        }

        string normalizedEmail = email.Trim().ToLowerInvariant();
        string sessionId = sessions.Create(verification.AccountId, normalizedEmail);

        return DashboardLoginResult.Authenticated(
            sessionId,
            new DashboardPrincipal(verification.AccountId, normalizedEmail, perms)
        );
    }

    public async Task<DashboardPrincipal?> ResolveAsync(string? sessionId, CancellationToken ct)
    {
        (int AccountId, string Email)? session = sessions.Resolve(sessionId);

        if (session is null)
        {
            return null;
        }

        PermissionSet perms = await permissions
            .ResolveForAccountAsync(session.Value.AccountId, ct)
            .ConfigureAwait(false);

        // Capabilities are re-checked every request: revoking a role takes effect immediately.
        if (!HasDashboardAccess(perms))
        {
            return null;
        }

        return new DashboardPrincipal(session.Value.AccountId, session.Value.Email, perms);
    }

    public void Logout(string? sessionId) => sessions.Remove(sessionId);

    private static bool HasDashboardAccess(PermissionSet permissions) =>
        permissions.IsSuperUser || permissions.HasAny(Capabilities.Dashboard.All);
}

internal enum DashboardLoginOutcome
{
    InvalidCredentials,
    Forbidden,

    /// <summary>Credentials were right and the account has a second factor the request did not carry.</summary>
    MfaRequired,

    /// <summary>Credentials were right, a code was supplied, and it did not verify.</summary>
    InvalidCode,
    Authenticated,
}

internal readonly record struct DashboardLoginResult(
    DashboardLoginOutcome Outcome,
    string? SessionId,
    DashboardPrincipal? Principal
)
{
    public static DashboardLoginResult InvalidCredentials { get; } =
        new(DashboardLoginOutcome.InvalidCredentials, null, null);

    public static DashboardLoginResult Forbidden { get; } =
        new(DashboardLoginOutcome.Forbidden, null, null);

    public static DashboardLoginResult MfaRequired { get; } =
        new(DashboardLoginOutcome.MfaRequired, null, null);

    public static DashboardLoginResult InvalidCode { get; } =
        new(DashboardLoginOutcome.InvalidCode, null, null);

    public static DashboardLoginResult Authenticated(
        string sessionId,
        DashboardPrincipal principal
    ) => new(DashboardLoginOutcome.Authenticated, sessionId, principal);
}
