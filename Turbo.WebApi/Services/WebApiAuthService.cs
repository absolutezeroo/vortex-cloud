using System;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
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
    IOptions<WebApiConfig> options
) : IWebApiAuthService
{
    private readonly IDbContextFactory<TurboDbContext> _db = dbCtxFactory;
    private readonly WebApiSessionStore _sessions = sessions;
    private readonly WebApiConfig _config = options.Value;

    public async Task<(bool Success, string? SessionId, string? Error)> LoginAsync(
        string email,
        string password,
        CancellationToken ct
    )
    {
        await using var db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        var account = await db
            .PlayerAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email.ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (account is null)
            return (false, null, "pocket.auth.login_failed");

        // BCrypt.Verify is CPU-bound; run off the thread pool to avoid blocking the listener loop.
        var valid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, account.PasswordHash), ct)
            .ConfigureAwait(false);

        if (!valid)
            return (false, null, "pocket.auth.login_failed");

        var sessionId = _sessions.CreateSession(account.Id);
        return (true, sessionId, null);
    }

    public async Task<(bool Success, int AccountId, string? Error)> RegisterAsync(
        string email,
        string password,
        CancellationToken ct
    )
    {
        await using var db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        var normalizedEmail = email.ToLowerInvariant();

        var exists = await db
            .PlayerAccounts.AsNoTracking()
            .AnyAsync(a => a.Email == normalizedEmail, ct)
            .ConfigureAwait(false);

        if (exists)
            return (false, 0, "pocket.auth.valid_email_required");

        // Work factor 12 is strong enough for production; run off the thread pool.
        var hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12), ct)
            .ConfigureAwait(false);

        var account = new PlayerAccountEntity
        {
            Email = normalizedEmail,
            PasswordHash = hash,
        };

        db.PlayerAccounts.Add(account);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return (true, account.Id, null);
    }

    public async Task<(bool Success, string? Ticket, string? Error)> GetSsoTokenAsync(
        int playerId,
        string ip,
        CancellationToken ct
    )
    {
        await using var db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        var player = await db
            .Players
            .FirstOrDefaultAsync(p => p.Id == playerId, ct)
            .ConfigureAwait(false);

        if (player is null)
            return (false, null, "pocket.auth.login_failed");

        var existing = await db
            .SecurityTickets.FirstOrDefaultAsync(t => t.PlayerEntityId == playerId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            db.SecurityTickets.Remove(existing);

        var ticket = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

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

        return (true, ticket, null);
    }
}
