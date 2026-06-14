using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Turbo.Authentication.Configuration;
using Turbo.Database.Context;
using Turbo.Primitives.Authentication;
using Turbo.Primitives.Events;

namespace Turbo.Authentication;

public sealed class AuthenticationService(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IEventPublisher events,
    IOptions<AuthenticationConfig> options
) : IAuthenticationService
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IEventPublisher _events = events;
    private readonly string _ipHashSecret = options.Value.IpHashSecret;

    public async Task<int> GetPlayerIdFromTicketAsync(
        string ticket,
        string? remoteIp = null,
        CancellationToken ct = default
    )
    {
        if (ticket is null || ticket.Length == 0)
            return 0;

        var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            if (dbCtx.SecurityTickets is null)
                return 0;

            var entity = await dbCtx
                .SecurityTickets.AsNoTracking()
                .FirstOrDefaultAsync(entity => entity.Ticket == ticket, ct)
                .ConfigureAwait(false);

            if (entity is null)
            {
                await _events
                    .PublishAsync(new PlayerLoginFailedEvent(HashIp(remoteIp)), ct)
                    .ConfigureAwait(false);

                return 0;
            }

            // check timestamp for expiration, if time now is greater than expiration, return 0;

            // TODO(dev): ticket deletion disabled for local testing — re-enable for production
            // if (!entity.IsLocked)
            // {
            //     dbCtx.SecurityTickets.Remove(entity);
            //     await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
            // }

            await _events
                .PublishAsync(new PlayerLoggedInEvent(entity.PlayerEntityId, HashIp(remoteIp)), ct)
                .ConfigureAwait(false);

            return entity.PlayerEntityId;
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    private string? HashIp(string? remoteIp)
    {
        if (string.IsNullOrWhiteSpace(remoteIp))
            return null;

        var key = _ipHashSecret;
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var ipBytes = Encoding.UTF8.GetBytes(remoteIp.Trim());

        var hash = HMACSHA256.HashData(keyBytes, ipBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
