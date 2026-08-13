using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Moderation;
using Vortex.Primitives.Moderation;

namespace Vortex.Rooms;

/// <summary>
/// Owns cfh_tickets and the cfh_categories/cfh_topics catalog. Ticket volume is low (staff/report
/// action rate, not a hot path) so every method opens its own short-lived context.
/// </summary>
internal sealed class CfhTicketService(IDbContextFactory<VortexDbContext> dbContextFactory)
    : ICfhTicketService
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;

    public async Task<int> CreateTicketAsync(
        int topicId,
        int reporterPlayerId,
        int reportedPlayerId,
        int? roomId,
        string message,
        IReadOnlyList<(int UserId, string Text)> evidence,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string? evidenceJson =
            evidence.Count == 0
                ? null
                : JsonSerializer.Serialize(
                    evidence.Select(e => new { userId = e.UserId, text = e.Text })
                );

        CfhTicketEntity ticket = new()
        {
            State = CfhTicketState.Open,
            CfhTopicEntityId = topicId,
            ReporterPlayerEntityId = reporterPlayerId,
            ReportedPlayerEntityId = reportedPlayerId,
            RoomEntityId = roomId,
            Message = message.Length > 500 ? message[..500] : message,
            EvidenceJson = evidenceJson,
        };

        dbCtx.CfhTickets.Add(ticket);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return ticket.Id;
    }

    public async Task PickTicketsAsync(
        IReadOnlyList<int> issueIds,
        int pickerPlayerId,
        CancellationToken ct = default
    )
    {
        if (issueIds.Count == 0)
        {
            return;
        }

        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<CfhTicketEntity> tickets = await dbCtx
            .CfhTickets.Where(t =>
                issueIds.Contains(t.Id) && t.State == CfhTicketState.Open && t.DeletedAt == null
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (CfhTicketEntity ticket in tickets)
        {
            ticket.State = CfhTicketState.Picked;
            ticket.PickerPlayerEntityId = pickerPlayerId;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<CfhTicketCloseOutcome>> CloseTicketsAsync(
        IReadOnlyList<int> issueIds,
        CfhTicketCloseReason reason,
        bool sanctioned,
        CancellationToken ct = default
    )
    {
        if (issueIds.Count == 0)
        {
            return [];
        }

        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<CfhTicketEntity> tickets = await dbCtx
            .CfhTickets.Where(t =>
                issueIds.Contains(t.Id) && t.State != CfhTicketState.Closed && t.DeletedAt == null
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ImmutableArray<CfhTicketCloseOutcome>.Builder outcomes =
            ImmutableArray.CreateBuilder<CfhTicketCloseOutcome>(tickets.Count);

        foreach (CfhTicketEntity ticket in tickets)
        {
            ticket.State = CfhTicketState.Closed;
            ticket.ClosedAt = System.DateTime.UtcNow;
            ticket.CloseReason = reason;
            ticket.Sanctioned = sanctioned;

            outcomes.Add(
                new CfhTicketCloseOutcome(
                    ticket.Id,
                    ticket.ReporterPlayerEntityId,
                    ticket.ReportedPlayerEntityId,
                    sanctioned
                )
            );
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return outcomes.MoveToImmutable();
    }

    public async Task ReleaseTicketsAsync(
        IReadOnlyList<int> issueIds,
        CancellationToken ct = default
    )
    {
        if (issueIds.Count == 0)
        {
            return;
        }

        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<CfhTicketEntity> tickets = await dbCtx
            .CfhTickets.Where(t =>
                issueIds.Contains(t.Id) && t.State == CfhTicketState.Picked && t.DeletedAt == null
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (CfhTicketEntity ticket in tickets)
        {
            ticket.State = CfhTicketState.Open;
            ticket.PickerPlayerEntityId = null;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<CfhTicketSummary?> GetTicketAsync(int issueId, CancellationToken ct = default)
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        return await dbCtx
            .CfhTickets.AsNoTracking()
            .Where(t => t.Id == issueId && t.DeletedAt == null)
            .Select(t => new CfhTicketSummary(
                t.Id,
                t.State,
                t.CfhTopicEntityId,
                t.ReporterPlayerEntityId,
                t.ReportedPlayerEntityId
            ))
            .Cast<CfhTicketSummary?>()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<CfhTicketEvidenceSnapshot?> GetTicketEvidenceAsync(
        int issueId,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        var row = await dbCtx
            .CfhTickets.AsNoTracking()
            .Where(t => t.Id == issueId && t.DeletedAt == null)
            .Select(t => new
            {
                t.RoomEntityId,
                t.CreatedAt,
                t.EvidenceJson,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        ImmutableArray<CfhEvidenceLine> evidence = row.EvidenceJson is null
            ? []
            : JsonSerializer
                .Deserialize<List<EvidenceLineDto>>(row.EvidenceJson)!
                .Select(e => new CfhEvidenceLine(e.userId, e.text))
                .ToImmutableArray();

        return new CfhTicketEvidenceSnapshot(row.RoomEntityId, row.CreatedAt, evidence);
    }

    private sealed record EvidenceLineDto(int userId, string text);

    public async Task<CfhTopicSnapshot?> GetTopicAsync(int topicId, CancellationToken ct = default)
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        return await dbCtx
            .CfhTopics.AsNoTracking()
            .Where(t => t.Id == topicId && t.DeletedAt == null)
            .Select(t => new CfhTopicSnapshot(
                t.Id,
                t.CfhCategoryEntityId,
                t.Name,
                t.Consequence,
                t.DefaultSanctionPresetEntityId
            ))
            .Cast<CfhTopicSnapshot?>()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ImmutableArray<CfhCategorySnapshot>> GetCatalogAsync(
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<CfhCategoryEntity> categories = await dbCtx
            .CfhCategories.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<CfhTopicEntity> topics = await dbCtx
            .CfhTopics.AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ImmutableArray<CfhCategorySnapshot>.Builder builder =
            ImmutableArray.CreateBuilder<CfhCategorySnapshot>(categories.Count);

        foreach (CfhCategoryEntity category in categories)
        {
            ImmutableArray<CfhTopicSnapshot> categoryTopics = topics
                .Where(t => t.CfhCategoryEntityId == category.Id)
                .Select(t => new CfhTopicSnapshot(
                    t.Id,
                    t.CfhCategoryEntityId,
                    t.Name,
                    t.Consequence,
                    t.DefaultSanctionPresetEntityId
                ))
                .ToImmutableArray();

            builder.Add(new CfhCategorySnapshot(category.Id, category.Name, categoryTopics));
        }

        return builder.MoveToImmutable();
    }

    public async Task<ImmutableArray<CfhIssueQueueEntrySnapshot>> GetOpenQueueAsync(
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        System.DateTime now = System.DateTime.UtcNow;

        var rows = await dbCtx
            .CfhTickets.AsNoTracking()
            .Where(t => t.State != CfhTicketState.Closed && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.State,
                t.CfhTopicEntity!.CfhCategoryEntityId,
                t.CreatedAt,
                t.ReporterPlayerEntityId,
                ReporterName = t.ReporterPlayerEntity != null ? t.ReporterPlayerEntity.Name : "",
                t.ReportedPlayerEntityId,
                ReportedName = t.ReportedPlayerEntity != null ? t.ReportedPlayerEntity.Name : "",
                t.PickerPlayerEntityId,
                PickerName = t.PickerPlayerEntity != null ? t.PickerPlayerEntity.Name : "",
                t.Message,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(r => new CfhIssueQueueEntrySnapshot(
                r.Id,
                r.State,
                r.CfhCategoryEntityId,
                (int)(now - r.CreatedAt).TotalMilliseconds,
                0,
                r.ReporterPlayerEntityId,
                r.ReporterName,
                r.ReportedPlayerEntityId,
                r.ReportedName,
                r.PickerPlayerEntityId ?? 0,
                r.PickerName,
                r.Message
            ))
            .ToImmutableArray();
    }

    public async Task<ImmutableArray<CfhPendingCallSnapshot>> GetPendingForReporterAsync(
        int reporterPlayerId,
        CancellationToken ct = default
    )
    {
        if (reporterPlayerId <= 0)
        {
            return [];
        }

        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        var rows = await dbCtx
            .CfhTickets.AsNoTracking()
            .Where(t =>
                t.ReporterPlayerEntityId == reporterPlayerId
                && t.State != CfhTicketState.Closed
                && t.DeletedAt == null
            )
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.CreatedAt,
                t.Message,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(r => new CfhPendingCallSnapshot
            {
                CallId = r.Id.ToString(CultureInfo.InvariantCulture),
                // Round-trip format: the client only ever prints this back, and a culture-dependent
                // one would read differently depending on the server's locale.
                TimeStamp = r.CreatedAt.ToString("s", CultureInfo.InvariantCulture),
                Message = r.Message ?? string.Empty,
            })
            .ToImmutableArray();
    }

    public async Task<int> DeletePendingForReporterAsync(
        int reporterPlayerId,
        CancellationToken ct = default
    )
    {
        if (reporterPlayerId <= 0)
        {
            return 0;
        }

        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<CfhTicketEntity> pending = await dbCtx
            .CfhTickets.Where(t =>
                t.ReporterPlayerEntityId == reporterPlayerId
                && t.State == CfhTicketState.Open
                && t.DeletedAt == null
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pending.Count == 0)
        {
            return 0;
        }

        System.DateTime now = System.DateTime.UtcNow;

        foreach (CfhTicketEntity ticket in pending)
        {
            // Soft-deleted rather than closed: a withdrawn report is not a moderation outcome, and
            // counting it as one would skew every "tickets handled" figure the dashboard draws.
            ticket.DeletedAt = now;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return pending.Count;
    }

    public async Task<ImmutableArray<PlayerSanctionSnapshot>> GetSanctionHistoryAsync(
        int playerId,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        // Bans hang off the account, not the character, so the player has to be resolved to one
        // first. A player with no account row simply has no history rather than an error: the
        // client's screen is "here is what you did", and it can honestly be empty.
        int? accountId = await dbCtx
            .Players.AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => p.PlayerAccountEntityId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (accountId is null or 0)
        {
            return [];
        }

        var bans = await dbCtx
            .AccountBans.AsNoTracking()
            .Where(b => b.PlayerAccountEntityId == accountId)
            .OrderByDescending(b => b.DateExpires)
            .Select(b => new
            {
                b.DateExpires,
                b.Reason,
                b.CreatedAt,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        System.DateTime now = System.DateTime.UtcNow;

        return
        [
            .. bans.Select(ban =>
            {
                bool active = ban.DateExpires > now;
                double hoursLeft = (ban.DateExpires - now).TotalHours;
                double duration = (ban.DateExpires - ban.CreatedAt).TotalHours;

                return new PlayerSanctionSnapshot
                {
                    // The client only special-cases three names and falls through to a generic
                    // timed ban for the rest, so a very long ban is reported as permanent rather
                    // than as a number of hours nobody will ever count down.
                    TypeName = duration >= PermanentBanHours ? "BAN_PERMANENT" : "BAN",
                    Reason = ban.Reason ?? string.Empty,
                    DurationHours = ToWholeHours(duration),
                    HoursLeft = active ? ToWholeHours(hoursLeft) : 0,
                    ExpiresAtUtc = ban.DateExpires,
                    IsActive = active,
                };
            }),
        ];
    }

    /// <summary>A ban this long is reported as permanent. Ten years, so no real sanction reaches it
    /// by accident and the "forever" rows that use DateTime.MaxValue land on the right side.</summary>
    private const double PermanentBanHours = 10 * 365 * 24;

    private static int ToWholeHours(double hours) =>
        hours <= 0 ? 0
        : hours >= int.MaxValue ? int.MaxValue
        : (int)System.Math.Ceiling(hours);
}
