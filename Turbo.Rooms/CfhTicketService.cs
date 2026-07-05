using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Moderation;
using Turbo.Primitives.Moderation;

namespace Turbo.Rooms;

/// <summary>
/// Owns cfh_tickets and the cfh_categories/cfh_topics catalog. Ticket volume is low (staff/report
/// action rate, not a hot path) so every method opens its own short-lived context.
/// </summary>
internal sealed class CfhTicketService(IDbContextFactory<TurboDbContext> dbContextFactory)
    : ICfhTicketService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;

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
        await using TurboDbContext dbCtx = await _dbContextFactory
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

        await using TurboDbContext dbCtx = await _dbContextFactory
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

        await using TurboDbContext dbCtx = await _dbContextFactory
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

        await using TurboDbContext dbCtx = await _dbContextFactory
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
        await using TurboDbContext dbCtx = await _dbContextFactory
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

    public async Task<CfhTopicSnapshot?> GetTopicAsync(int topicId, CancellationToken ct = default)
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
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
        await using TurboDbContext dbCtx = await _dbContextFactory
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
}
