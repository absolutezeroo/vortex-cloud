using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Events;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.Habbicons.Grains;

/// <summary>
/// Using a Habbicon — in a room, or inside a private conversation.
/// </summary>
/// <remarks>
/// <para>
/// Ownership, existence and the rate limit are settled here; whether the <em>place</em> allows it is
/// settled by the place. The room applies its own mute and flood gate, the messenger applies its
/// friend and block rules, and neither of them learns anything about Habbicons to do it. That split
/// is what keeps a second communication policy from growing here and disagreeing with the first.
/// </para>
/// <para>
/// Nothing is recorded until the delivery succeeded. A use the room refused is not a use, and
/// counting it would let a muted player advance a "use Habbicons" task by clicking at a wall.
/// </para>
/// </remarks>
internal sealed partial class PlayerHabbiconGrain
{
    public async Task UseInRoomAsync(int roomId, int habbiconId, CancellationToken ct)
    {
        if (roomId <= 0 || !TryBeginUse(habbiconId, out HabbiconDefinitionSnapshot? definition))
        {
            return;
        }

        bool shown = await grainFactory
            .GetRoomAvatars((RoomId)roomId)
            .UseHabbiconAsync(PlayerId, habbiconId)
            .ConfigureAwait(true);

        // The room refused it -- not standing there, muted, flooding. Nothing was seen, so nothing
        // is recorded and no event goes out: this is the line that stops a "use Habbicons" task
        // being farmed by a muted player clicking at a wall.
        if (!shown)
        {
            return;
        }

        await RecordUseAsync(definition, roomId, null, ct).ConfigureAwait(true);
    }

    public async Task UseInConversationAsync(
        int conversationPlayerId,
        int habbiconId,
        int confirmationId,
        CancellationToken ct
    )
    {
        if (
            conversationPlayerId <= 0
            || !TryBeginUse(habbiconId, out HabbiconDefinitionSnapshot? definition)
        )
        {
            return;
        }

        // Straight through the messenger's own send path: a Habbicon in a conversation *is* a
        // message as far as the client is concerned, so it gets the friend check, the block check,
        // the history row and the delivery that a line of text gets, rather than a parallel route
        // that could let one through where the other would not.
        InstantMessageErrorCodeType? error = await grainFactory
            .GetMessengerGrain(PlayerId)
            .SendMessageAsync(
                conversationPlayerId,
                string.Empty,
                conversationPlayerId,
                confirmationId,
                habbiconId,
                ct
            )
            .ConfigureAwait(true);

        if (error.HasValue)
        {
            return;
        }

        await RecordUseAsync(definition, 0, conversationPlayerId, ct).ConfigureAwait(true);
    }

    /// <summary>
    /// The checks common to both routes: the Habbicon exists, this player owns it in a state that
    /// can be used, and they are not clicking faster than the cooldown allows.
    /// </summary>
    /// <remarks>
    /// The rate limit is separate from the room's flood control on purpose. Flood control covers a
    /// room; a private conversation has none, and without this a selector held down would spray a
    /// friend's console.
    /// </remarks>
    private bool TryBeginUse(
        int habbiconId,
        [NotNullWhen(true)] out HabbiconDefinitionSnapshot? definition
    )
    {
        definition = null;

        if (!catalog.TryGetHabbicon(habbiconId, out HabbiconDefinitionSnapshot? found))
        {
            return false;
        }

        // Never the id the client sent, always the row we hold: the selector shows what it was told
        // it owns, and a crafted packet can name anything at all.
        if (
            !_owned.TryGetValue(habbiconId, out OwnedHabbicon? owned)
            || !HabbiconCollectionRules.IsUsable(owned.State)
        )
        {
            return false;
        }

        long now = Environment.TickCount64;

        if (_useCooldownMs > 0 && now - _lastUseMs < _useCooldownMs)
        {
            return false;
        }

        _lastUseMs = now;
        definition = found;

        return true;
    }

    /// <summary>
    /// Marks the Habbicon as just used — which is what the "recently used" row is built from — and
    /// tells the hotel about it.
    /// </summary>
    private async Task RecordUseAsync(
        HabbiconDefinitionSnapshot definition,
        int roomId,
        int? conversationPlayerId,
        CancellationToken ct
    )
    {
        DateTime now = DateTime.UtcNow;

        try
        {
            await using VortexDbContext db = await dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            PlayerHabbiconEntity? row = await db
                .PlayerHabbicons.FirstOrDefaultAsync(
                    h =>
                        h.PlayerEntityId == PlayerId && h.HabbiconEntityId == definition.HabbiconId,
                    ct
                )
                .ConfigureAwait(true);

            if (row is not null)
            {
                row.LastUsedAt = now;
                await db.SaveChangesAsync(ct).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            // The Habbicon was already shown. Losing the timestamp costs a place in the recents
            // row and nothing else, so it is not worth failing the use over -- but it is worth
            // knowing about.
            logger.LogError(
                ex,
                "Failed to record the use of Habbicon {HabbiconId} by player {PlayerId}.",
                definition.HabbiconId,
                PlayerId
            );
        }

        if (_owned.TryGetValue(definition.HabbiconId, out OwnedHabbicon? cached))
        {
            _owned[definition.HabbiconId] = cached with { LastUsedAt = now };
        }

        await events
            .PublishAsync(
                new HabbiconUsedEvent(
                    PlayerId,
                    definition.HabbiconId,
                    definition.CollectionId,
                    roomId,
                    conversationPlayerId is > 0 ? (PlayerId)conversationPlayerId.Value : null
                ),
                ct
            )
            .ConfigureAwait(true);
    }
}
