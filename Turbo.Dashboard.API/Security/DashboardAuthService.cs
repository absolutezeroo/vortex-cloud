using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Authentication;
using Turbo.Primitives.Permissions;

namespace Turbo.Dashboard.API.Security;

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
    private static readonly string[] DashboardCapabilities =
    [
        Capabilities.Dashboard.OverviewRead,
        Capabilities.Dashboard.AuditRead,
        Capabilities.Dashboard.EconomyRead,
        Capabilities.Dashboard.PlayersRead,
        Capabilities.Dashboard.FurnitureRead,
        Capabilities.Dashboard.OpsGrantCurrency,
        Capabilities.Dashboard.OpsGrantItem,
        Capabilities.Dashboard.OpsKickPlayer,
        Capabilities.Dashboard.OpsManageVouchers,
        Capabilities.Dashboard.OpsBanAccount,
        Capabilities.Dashboard.OpsMutePlayer,
        Capabilities.Dashboard.OpsTradingLock,
        Capabilities.Dashboard.OpsCfhManage,
        Capabilities.Dashboard.OpsRoomsManage,
    ];

    public async Task<DashboardLoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken ct
    )
    {
        int? accountId = await authenticator
            .VerifyCredentialsAsync(email, password, ct)
            .ConfigureAwait(false);

        if (accountId is null)
        {
            return DashboardLoginResult.InvalidCredentials;
        }

        PermissionSet perms = await permissions
            .ResolveForAccountAsync(accountId.Value, ct)
            .ConfigureAwait(false);

        if (!HasDashboardAccess(perms))
        {
            return DashboardLoginResult.Forbidden;
        }

        string normalizedEmail = email.Trim().ToLowerInvariant();
        string sessionId = sessions.Create(accountId.Value, normalizedEmail);

        return DashboardLoginResult.Authenticated(
            sessionId,
            new DashboardPrincipal(accountId.Value, normalizedEmail, perms)
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
        permissions.IsSuperUser || permissions.HasAny(DashboardCapabilities);
}

internal enum DashboardLoginOutcome
{
    InvalidCredentials,
    Forbidden,
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

    public static DashboardLoginResult Authenticated(
        string sessionId,
        DashboardPrincipal principal
    ) => new(DashboardLoginOutcome.Authenticated, sessionId, principal);
}
