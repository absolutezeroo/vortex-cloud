using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Primitives.Authentication;
using Turbo.Primitives.Observability;

namespace Turbo.Authentication;

public sealed class AuthenticationService(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IAuditSink auditSink
) : IAuthenticationService
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IAuditSink _auditSink = auditSink;

    public async Task<int> GetPlayerIdFromTicketAsync(string ticket, CancellationToken ct = default)
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
                _auditSink.Emit(
                    new AuditEvent
                    {
                        Category = AuditCategory.Auth,
                        Action = "auth.login.failed",
                        Severity = AuditSeverity.Notice,
                        Result = AuditResult.Failed,
                    }
                );

                return 0;
            }

            // check timestamp for expiration, if time now is greater than expiration, return 0;

            // TODO(dev): ticket deletion disabled for local testing — re-enable for production
            // if (!entity.IsLocked)
            // {
            //     dbCtx.SecurityTickets.Remove(entity);
            //     await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
            // }

            _auditSink.Emit(
                new AuditEvent
                {
                    Category = AuditCategory.Auth,
                    Action = "auth.login.success",
                    Severity = AuditSeverity.Info,
                    Result = AuditResult.Success,
                    ActorPlayerId = entity.PlayerEntityId,
                }
            );

            return entity.PlayerEntityId;
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
