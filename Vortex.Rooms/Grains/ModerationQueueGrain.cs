using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Moderation.Grains;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Serializes every CFH ticket transition and pushes the result to the moderators watching the
/// queue. See <see cref="IModerationQueueGrain"/> for why this is a grain and not a service.
/// </summary>
[KeepAlive]
public sealed class ModerationQueueGrain(
    ICfhTicketService tickets,
    IEventPublisher events,
    IGrainFactory grainFactory,
    ILogger<IModerationQueueGrain> logger
) : Grain, IModerationQueueGrain
{
    private readonly ICfhTicketService _tickets = tickets;
    private readonly IEventPublisher _events = events;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<IModerationQueueGrain> _logger = logger;

    /// <summary>
    /// Moderators currently holding the tool open. Rebuilt from logins rather than persisted: a
    /// subscription describes a live client, so surviving a restart would be wrong — every session
    /// it named is gone. Everyone still connected re-registers on their next login.
    /// </summary>
    private readonly HashSet<PlayerId> _subscribers = [];

    public Task SubscribeAsync(PlayerId moderatorId)
    {
        if (moderatorId.Value > 0)
        {
            _subscribers.Add(moderatorId);
        }

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(PlayerId moderatorId)
    {
        _subscribers.Remove(moderatorId);
        _inspectedRooms.Remove(moderatorId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The room each moderator last opened the room tool for. Bounded by the number of moderators,
    /// and retired with their subscription on disconnect.
    /// </summary>
    private readonly Dictionary<PlayerId, int> _inspectedRooms = [];

    public Task NoteInspectedRoomAsync(PlayerId moderatorId, int roomId)
    {
        if (moderatorId.Value > 0 && roomId > 0)
        {
            _inspectedRooms[moderatorId] = roomId;
        }

        return Task.CompletedTask;
    }

    public Task<int> GetInspectedRoomAsync(PlayerId moderatorId) =>
        Task.FromResult(_inspectedRooms.GetValueOrDefault(moderatorId));

    public async Task PublishTicketOpenedAsync(int issueId)
    {
        if (_subscribers.Count == 0)
        {
            return;
        }

        await PublishIssueBlocksAsync([issueId], CancellationToken.None).ConfigureAwait(true);
    }

    public async Task PickAsync(
        PlayerId pickerId,
        IReadOnlyList<int> issueIds,
        bool retryEnabled,
        int retryCount,
        CancellationToken ct
    )
    {
        ImmutableArray<CfhTicketPickOutcome> outcomes = await _tickets
            .PickTicketsAsync(issueIds, pickerId.Value, ct)
            .ConfigureAwait(true);

        int[] acquired = [.. outcomes.Where(o => o.Acquired).Select(o => o.IssueId)];
        ImmutableArray<CfhTicketPickOutcome> lost = [.. outcomes.Where(o => !o.Acquired)];

        if (acquired.Length > 0)
        {
            await _events
                .PublishAsync(new CfhTicketsPickedEvent(pickerId.Value, acquired), ct)
                .ConfigureAwait(true);

            // Everyone watching, the picker included: the issue block now names its holder, which
            // is how the other tools grey it out instead of offering it again.
            await PublishIssueBlocksAsync(acquired, ct).ConfigureAwait(true);
        }

        if (lost.Length == 0)
        {
            return;
        }

        // Only the moderator who reached for them hears about a failed pick. The client keys its
        // retry off this message, so sending it to the room at large would set everyone retrying.
        await TrySendAsync(
                pickerId,
                new IssuePickFailedMessageComposer
                {
                    Conflicts =
                    [
                        .. lost.Select(o => new IssuePickConflict
                        {
                            IssueId = o.IssueId,
                            PickerUserId = o.PickerPlayerId,
                            PickerUserName = o.PickerPlayerName,
                        }),
                    ],
                    RetryEnabled = retryEnabled,
                    RetryCount = retryCount,
                }
            )
            .ConfigureAwait(true);
    }

    public async Task ReleaseAsync(
        PlayerId actorId,
        IReadOnlyList<int> issueIds,
        CancellationToken ct
    )
    {
        ImmutableArray<int> released = await _tickets
            .ReleaseTicketsAsync(issueIds, ct)
            .ConfigureAwait(true);

        if (released.Length == 0)
        {
            return;
        }

        await _events
            .PublishAsync(new CfhTicketsReleasedEvent(actorId.Value, [.. released]), ct)
            .ConfigureAwait(true);

        await PublishIssueBlocksAsync(released, ct).ConfigureAwait(true);
    }

    public async Task<int> WithdrawForReporterAsync(PlayerId reporterId, CancellationToken ct)
    {
        ImmutableArray<int> withdrawn = await _tickets
            .DeletePendingForReporterAsync(reporterId.Value, ct)
            .ConfigureAwait(true);

        if (withdrawn.Length == 0)
        {
            return 0;
        }

        await BroadcastAsync([
                .. withdrawn.Select(id => new IssueDeletedMessageComposer { IssueId = id }),
            ])
            .ConfigureAwait(true);

        return withdrawn.Length;
    }

    public async Task<ImmutableArray<CfhTicketCloseOutcome>> CloseAsync(
        PlayerId actorId,
        IReadOnlyList<int> issueIds,
        CfhTicketCloseReason reason,
        bool sanctioned,
        CancellationToken ct
    )
    {
        ImmutableArray<CfhTicketCloseOutcome> outcomes = await _tickets
            .CloseTicketsAsync(issueIds, reason, sanctioned, ct)
            .ConfigureAwait(true);

        if (outcomes.Length == 0)
        {
            return outcomes;
        }

        await _events
            .PublishAsync(
                new CfhTicketsClosedEvent(
                    actorId.Value,
                    [.. outcomes.Select(o => o.IssueId)],
                    reason.ToString(),
                    sanctioned
                ),
                ct
            )
            .ConfigureAwait(true);

        await BroadcastAsync([
                .. outcomes.Select(o => new IssueDeletedMessageComposer { IssueId = o.IssueId }),
            ])
            .ConfigureAwait(true);

        return outcomes;
    }

    /// <summary>Pushes the current issue block for each id to every watching moderator.</summary>
    private async Task PublishIssueBlocksAsync(IReadOnlyList<int> issueIds, CancellationToken ct)
    {
        if (_subscribers.Count == 0)
        {
            return;
        }

        ImmutableArray<CfhIssueQueueEntrySnapshot> entries = await _tickets
            .GetQueueEntriesAsync(issueIds, ct)
            .ConfigureAwait(true);

        if (entries.Length == 0)
        {
            return;
        }

        await BroadcastAsync([.. entries.Select(e => new IssueInfoMessageComposer { Issue = e })])
            .ConfigureAwait(true);
    }

    /// <summary>
    /// Fans one batch of composers out to every subscriber at once. A moderator whose delivery
    /// fails is dropped rather than retried — the only way it fails is that they are gone, and
    /// keeping them would make every later broadcast pay for the same dead entry.
    /// </summary>
    private async Task BroadcastAsync(IComposer[] composers)
    {
        if (composers.Length == 0 || _subscribers.Count == 0)
        {
            return;
        }

        PlayerId[] targets = [.. _subscribers];

        // Independent grains, so one round trip wall-clock rather than one per moderator.
        bool[] delivered = await Task.WhenAll(targets.Select(id => TrySendAsync(id, composers)))
            .ConfigureAwait(true);

        for (int i = 0; i < targets.Length; i++)
        {
            if (!delivered[i])
            {
                _subscribers.Remove(targets[i]);
            }
        }
    }

    /// <summary>Delivers to one moderator, reporting failure instead of raising it: a broadcast is
    /// not allowed to fail because one recipient went away mid-flight.</summary>
    private async Task<bool> TrySendAsync(PlayerId moderatorId, params IComposer[] composers)
    {
        try
        {
            await _grainFactory
                .GetPlayerPresenceGrain(moderatorId.Value)
                .SendComposerAsync(composers)
                .ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deliver a moderation queue update to moderator {ModeratorId}; dropping their subscription.",
                moderatorId.Value
            );

            return false;
        }
    }
}
