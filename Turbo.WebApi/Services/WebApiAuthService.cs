using System;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Security;
using Turbo.WebApi.Configuration;
using Turbo.WebApi.Session;

namespace Turbo.WebApi.Services;

public sealed class WebApiAuthService(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    WebApiSessionStore sessions,
    IOptions<WebApiConfig> options,
    ILogger<WebApiAuthService> logger
) : IWebApiAuthService
{
    private readonly IDbContextFactory<TurboDbContext> _db = dbCtxFactory;
    private readonly WebApiSessionStore _sessions = sessions;
    private readonly WebApiConfig _config = options.Value;
    private readonly ILogger<WebApiAuthService> _logger = logger;

    // Pre-computed hash used when no account matches — keeps response time constant
    // regardless of whether the email exists, preventing user enumeration via timing.
    private const string DummyHash = "$2a$12$C6UzMDM.H6dfI/f/IKcEeO3qj8b1l1u8j0Y9o6m4w8h2tY6q0Q1Qe";

    public async Task<(bool Success, string? SessionId, int AccountId, string? Error)> LoginAsync(
        string email,
        string password,
        CancellationToken ct
    )
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        PlayerAccountEntity? account = await db
            .PlayerAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email.ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        string hash = account?.PasswordHash ?? DummyHash;

        // BCrypt.Verify is CPU-bound; run off the thread pool to avoid blocking the listener loop.
        bool valid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, hash), ct)
            .ConfigureAwait(false);

        if (!valid || account is null)
        {
            _logger.LogWarning("Login failed for {Email}", email.ToLowerInvariant());
            return (false, null, 0, "pocket.auth.login_failed");
        }

        string sessionId = _sessions.CreateSession(account.Id);
        _logger.LogInformation(
            "Account {AccountId} authenticated ({Email})",
            account.Id,
            account.Email
        );
        return (true, sessionId, account.Id, null);
    }

    public async Task<(bool Success, int AccountId, string? Error)> RegisterAsync(
        string email,
        string password,
        CancellationToken ct
    )
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

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
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

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
