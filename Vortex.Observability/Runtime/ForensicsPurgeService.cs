using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Runtime;

/// <inheritdoc />
public sealed class ForensicsPurgeService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ILogger<ForensicsPurgeService> logger
) : IForensicsPurgeService
{
    /// <summary>
    /// The audit actions whose <c>Data</c> carries text the player themselves wrote or chose: a
    /// motto, a look, a name. Everything else in the trail is ids and numbers the hotel produced,
    /// which is the accounting record rather than personal content.
    /// </summary>
    private static readonly string[] FreeTextActions =
    [
        "profile.motto_changed",
        "profile.figure_changed",
        "profile.wardrobe_saved",
        "security.name.changed",
    ];

    /// <summary>
    /// What a scrubbed payload becomes. Not null: an empty payload and a removed one look the same
    /// from a query, and an operator reading the row deserves to know the difference between "this
    /// action carried nothing" and "this was erased on request".
    /// </summary>
    private const string ScrubbedPayload = """{"purged":true}""";

    public async Task<ForensicsPurgeResult> PurgePlayerAsync(
        int playerId,
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        // Their own words go entirely.
        int chatDeleted = await dbCtx
            .Chatlogs.Where(c => c.PlayerEntityId == playerId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        // Somebody else's words that happened to name them: the message belongs to its author and
        // stays, only the pointer at this player goes. Deleting the line instead would erase another
        // person's record on this person's request.
        int mentionsCleared = await dbCtx
            .Chatlogs.Where(c => c.TargetPlayerEntityId == playerId)
            .ExecuteUpdateAsync(up => up.SetProperty(c => c.TargetPlayerEntityId, (int?)null), ct)
            .ConfigureAwait(false);

        int visitsDeleted = await dbCtx
            .RoomEntryLogs.Where(e => e.PlayerEntityId == playerId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        // The hashed IP ties the account to a machine. It is the most personal thing in the audit
        // trail and the least load-bearing: nothing downstream reads it except the operator looking
        // for shared devices, which is exactly the linkage being asked to forget.
        int ipHashesCleared = await dbCtx
            .AuditEvents.Where(a => a.ActorPlayerId == playerId && a.IpHash != null)
            .ExecuteUpdateAsync(up => up.SetProperty(a => a.IpHash, (string?)null), ct)
            .ConfigureAwait(false);

        int payloadsScrubbed = await dbCtx
            .AuditEvents.Where(a =>
                a.ActorPlayerId == playerId && a.Data != null && FreeTextActions.Contains(a.Action)
            )
            .ExecuteUpdateAsync(up => up.SetProperty(a => a.Data, ScrubbedPayload), ct)
            .ConfigureAwait(false);

        ForensicsPurgeResult result = new()
        {
            ChatMessagesDeleted = chatDeleted,
            ChatMentionsCleared = mentionsCleared,
            RoomVisitsDeleted = visitsDeleted,
            AuditIpHashesCleared = ipHashesCleared,
            AuditPayloadsScrubbed = payloadsScrubbed,
        };

        logger.LogWarning(
            "Forensics purge for player {PlayerId}: {Chat} chat line(s) deleted, {Mentions} mention(s) cleared, "
                + "{Visits} visit(s) deleted, {IpHashes} IP hash(es) cleared, {Payloads} payload(s) scrubbed.",
            playerId,
            chatDeleted,
            mentionsCleared,
            visitsDeleted,
            ipHashesCleared,
            payloadsScrubbed
        );

        return result;
    }
}
