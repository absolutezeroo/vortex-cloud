using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;

namespace Turbo.Observability.Dashboard.Security;

internal sealed class DashboardAccessPolicy(IOptions<ObservabilityConfig> options)
{
    private readonly ObservabilityConfig _config = options.Value;

    private bool UseLegacyFallbackTokens =>
        string.IsNullOrWhiteSpace(_config.DashboardAdminToken)
        && string.IsNullOrWhiteSpace(_config.DashboardEconomyToken)
        && string.IsNullOrWhiteSpace(_config.DashboardModeratorToken);

    public DashboardRole ResolveRole(string? provided)
    {
        if (string.IsNullOrWhiteSpace(provided))
            return DashboardRole.None;

        if (UseLegacyFallbackTokens && TokenMatches(provided, _config.DashboardToken))
            return DashboardRole.Admin;

        if (TokenMatches(provided, _config.DashboardAdminToken))
            return DashboardRole.Admin;

        if (TokenMatches(provided, _config.DashboardEconomyToken))
            return DashboardRole.Economy;

        if (TokenMatches(provided, _config.DashboardModeratorToken))
            return DashboardRole.Moderator;

        if (TokenMatches(provided, _config.DashboardToken))
            return DashboardRole.Viewer;

        return DashboardRole.None;
    }

    public bool CanReadOverview(DashboardRole role) => role != DashboardRole.None;

    public bool CanReadAudit(DashboardRole role) =>
        role
            is DashboardRole.Viewer
                or DashboardRole.Moderator
                or DashboardRole.Economy
                or DashboardRole.Admin;

    public bool CanReadEconomy(DashboardRole role) =>
        role is DashboardRole.Economy or DashboardRole.Admin;

    private static bool TokenMatches(string provided, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken))
            return false;

        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(expectedToken));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(provided));

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
