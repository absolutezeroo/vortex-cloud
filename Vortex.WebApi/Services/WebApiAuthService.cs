using System;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Security;
using Vortex.Primitives.Authentication;
using Vortex.WebApi.Configuration;
using Vortex.WebApi.Session;

namespace Vortex.WebApi.Services;

public sealed class WebApiAuthService(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IAccountAuthenticator authenticator,
    WebApiSessionStore sessions,
    IOptions<WebApiConfig> options,
    ILogger<WebApiAuthService> logger
) : IWebApiAuthService
{
    private readonly IDbContextFactory<VortexDbContext> _db = dbCtxFactory;
    private readonly IAccountAuthenticator _authenticator = authenticator;
    private readonly WebApiSessionStore _sessions = sessions;
    private readonly WebApiConfig _config = options.Value;
    private readonly ILogger<WebApiAuthService> _logger = logger;

    public async Task<(bool Success, string? SessionId, int AccountId, string? Error)> LoginAsync(
        string email,
        string password,
        string? code,
        CancellationToken ct
    )
    {
        // This used to be its own copy of the password check -- same table, same dummy hash, same
        // off-thread BCrypt as Vortex.Authentication. Two copies is how the second factor came to
        // guard the admin cookie while the same password still opened this login and, through it, an
        // SSO ticket into the game. One entry point, and the factor comes with it.
        AccountVerification verification = await _authenticator
            .VerifyCredentialsAsync(email, password, code, ct)
            .ConfigureAwait(false);

        switch (verification.Outcome)
        {
            case AccountVerificationOutcome.MfaRequired:
                return (false, null, 0, "pocket.auth.mfa_required");

            case AccountVerificationOutcome.InvalidCode:
                _logger.LogWarning("Second factor rejected for {Email}", email.ToLowerInvariant());
                return (false, null, 0, "pocket.auth.invalid_code");

            case AccountVerificationOutcome.InvalidCredentials:
                _logger.LogWarning("Login failed for {Email}", email.ToLowerInvariant());
                return (false, null, 0, "pocket.auth.login_failed");
        }

        string sessionId = _sessions.CreateSession(verification.AccountId);
        _logger.LogInformation(
            "Account {AccountId} authenticated ({Email})",
            verification.AccountId,
            email.ToLowerInvariant()
        );

        return (true, sessionId, verification.AccountId, null);
    }

    public async Task<(bool Success, int AccountId, string? Error)> RegisterAsync(
        string email,
        string password,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        string normalizedEmail = email.ToLowerInvariant();

        bool exists = await db
            .PlayerAccounts.AsNoTracking()
            .AnyAsync(a => a.Email == normalizedEmail, ct)
            .ConfigureAwait(false);

        if (exists)
        {
            _logger.LogWarning(
                "Registration refused: email already taken ({Email})",
                normalizedEmail
            );
            return (false, 0, "pocket.auth.email_already_taken");
        }

        // Work factor 12 is strong enough for production; run off the thread pool.
        string? hash = await Task.Run(
                () => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                ct
            )
            .ConfigureAwait(false);

        PlayerAccountEntity account = new PlayerAccountEntity
        {
            Email = normalizedEmail,
            PasswordHash = hash,
        };

        db.PlayerAccounts.Add(account);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Account {AccountId} registered ({Email})",
            account.Id,
            normalizedEmail
        );
        return (true, account.Id, null);
    }

    public async Task<(bool Success, string? Ticket, string? Error)> GetSsoTokenAsync(
        int playerId,
        string ip,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        PlayerEntity? player = await db
            .Players.FirstOrDefaultAsync(p => p.Id == playerId, ct)
            .ConfigureAwait(false);

        if (player is null)
        {
            _logger.LogWarning("SSO token requested for unknown player {PlayerId}", playerId);
            return (false, null, "pocket.auth.login_failed");
        }

        SecurityTicketEntity? existing = await db
            .SecurityTickets.FirstOrDefaultAsync(t => t.PlayerEntityId == playerId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            db.SecurityTickets.Remove(existing);
        }

        string ticket = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        db.SecurityTickets.Add(
            new SecurityTicketEntity
            {
                PlayerEntityId = playerId,
                Ticket = ticket,
                IpAddress = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip,
                IsLocked = false,
                PlayerEntity = player,
            }
        );

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("SSO token issued for player {PlayerId}", playerId);
        return (true, ticket, null);
    }
}
