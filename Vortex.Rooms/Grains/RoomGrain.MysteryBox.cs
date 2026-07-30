using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Messages.Outgoing.Mysterybox;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Grains;
using Vortex.Primitives.MysteryBox.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The two-player mystery box open flow. Both halves are held by different players, so the pairing
/// lives on the room grain -- the single-threaded coordinator both sides already share -- which is
/// what keeps two key holders from claiming the same box or one key from opening two boxes.
/// </summary>
public sealed partial class RoomGrain
{
    public async Task UseMysteryBoxAsync(
        ActionContext ctx,
        RoomObjectId boxObjectId,
        CancellationToken ct
    )
    {
        PlayerId actorId = ctx.PlayerId;

        if (!_state.ItemsById.TryGetValue(boxObjectId, out IRoomItem? box))
        {
            return;
        }

        // A box wears its colour as its state, so the pairing colour comes from the item itself.
        string boxColor = MysteryBoxSprite.ColorFromState(box.Logic.GetState());

        if (boxColor.Length == 0)
        {
            // State 0: the box was created without a colour, so no key can ever match it. Say so
            // rather than silently doing nothing on every click.
            _logger.LogWarning(
                "Mystery box {ObjectId} (definition {DefinitionId}) is in the colourless state 0 and can never be paired with a key.",
                boxObjectId.Value,
                box.Definition.Id
            );

            return;
        }

        PlayerId ownerId = box.OwnerId;
        RoomMysteryBoxSession session = ResolveSession(box, ownerId, boxColor);

        if (session.BoxObjectId != boxObjectId)
        {
            // The owner already has another box waiting. The cancel message only carries an owner
            // id, so a second concurrent session would be un-cancellable.
            _logger.LogDebug(
                "Player {ActorId} clicked mystery box {BoxObjectId} while its owner already has box {ActiveBoxObjectId} waiting in room {RoomId}.",
                actorId.Value,
                boxObjectId.Value,
                session.BoxObjectId.Value,
                _state.RoomId.Value
            );

            return;
        }

        if (actorId == ownerId)
        {
            session.OwnerWaiting = true;
        }
        else
        {
            if (session.KeyHolderId is not null && session.KeyHolderId != actorId)
            {
                // Someone else is already holding this box open; they get first refusal.
                return;
            }

            bool hasKey = await _grainFactory
                .GetPlayerMysteryBoxGrain(actorId)
                .HasKeyAsync(session.Color, ct)
                .ConfigureAwait(true);

            if (!hasKey)
            {
                // Nobody joined, so don't leave an empty session behind for the sweep to clear.
                if (session.IsAbandoned)
                {
                    _state.MysteryBoxSessionsByOwnerId.Remove(ownerId);
                }

                return;
            }

            session.KeyHolderId = actorId;
        }

        session.StartedAtMs = NowMs();

        if (!session.IsReadyToOpen)
        {
            // Second frame of the colour's group: the room can see the box is in play rather than
            // wondering why nobody else can start it.
            await SetMysteryBoxStateAsync(box, MysteryBoxSprite.WaitingState(session.Color))
                .ConfigureAwait(true);

            await SendMysteryBoxComposerAsync(actorId, new ShowMysteryBoxWaitMessageComposer())
                .ConfigureAwait(true);

            return;
        }

        await OpenMysteryBoxAsync(ctx, session, ct).ConfigureAwait(true);
    }

    public async Task CancelMysteryBoxWaitAsync(
        ActionContext ctx,
        PlayerId boxOwnerId,
        CancellationToken ct
    )
    {
        PlayerId actorId = ctx.PlayerId;

        if (
            !_state.MysteryBoxSessionsByOwnerId.TryGetValue(
                boxOwnerId,
                out RoomMysteryBoxSession? session
            )
        )
        {
            return;
        }

        if (!session.IsParticipant(actorId))
        {
            return;
        }

        if (actorId == session.BoxOwnerId)
        {
            // The owner walking away ends the whole thing; the key holder's dialog has to close too,
            // otherwise they sit in front of a box that will never open.
            await EndMysteryBoxSessionAsync(session, notifyParticipants: true).ConfigureAwait(true);

            return;
        }

        session.KeyHolderId = null;

        await SendMysteryBoxComposerAsync(actorId, new CancelMysteryBoxWaitMessageComposer())
            .ConfigureAwait(true);

        if (session.IsAbandoned)
        {
            _state.MysteryBoxSessionsByOwnerId.Remove(session.BoxOwnerId);
        }

        await Task.CompletedTask.ConfigureAwait(true);
    }

    public async Task OpenMysteryTrophyAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        string inscription,
        CancellationToken ct
    )
    {
        if (!_state.ItemsById.TryGetValue(objectId, out IRoomItem? trophy))
        {
            return;
        }

        // Only the owner may inscribe: the trophy is consumed and the prize lands in their
        // inventory, so letting a visitor trigger it would spend someone else's furniture.
        if (trophy.OwnerId != ctx.PlayerId)
        {
            return;
        }

        MysteryBoxPrizeSnapshot? prize = await _grainFactory
            .GetMysteryBoxManagerGrain()
            .PickPrizeAsync(MysteryBoxPrizePool.Trophy, string.Empty, ct)
            .ConfigureAwait(true);

        if (prize is null)
        {
            return;
        }

        string ownerName = trophy.OwnerName;

        if (string.IsNullOrEmpty(ownerName))
        {
            ownerName = await _grainFactory
                .GetPlayerDirectoryGrain()
                .GetPlayerNameAsync(trophy.OwnerId, ct)
                .ConfigureAwait(true);
        }

        int maxLength = Math.Max(0, _roomConfig.MysteryTrophyInscriptionMaxLength);
        string trimmed = inscription.Length > maxLength ? inscription[..maxLength] : inscription;

        string legacyData = MysteryTrophyStuffData.Build(
            ownerName,
            DateOnly.FromDateTime(DateTime.UtcNow),
            trimmed
        );

        // Consume first: if the grant then fails the player is out a mystery trophy, but the reverse
        // order would let a repeated click mint trophies from one furniture.
        if (!await ConsumeMysteryFurnitureAsync(ctx, trophy, ct).ConfigureAwait(true))
        {
            return;
        }

        await _grainFactory
            .GetInventoryGrain(trophy.OwnerId.Value)
            .GrantFurnitureWithLegacyStuffDataAsync(prize.FurnitureDefinitionId, legacyData, ct)
            .ConfigureAwait(true);

        await _events
            .PublishAsync(
                new MysteryTrophyOpenedEvent(
                    _state.RoomId.Value,
                    objectId.Value,
                    trophy.OwnerId.Value,
                    prize.Id,
                    prize.FurnitureDefinitionId
                ),
                ct
            )
            .ConfigureAwait(true);
    }

    /// <summary>Tick-driven sweep. The client's wait dialog has no timer, so a player who wandered
    /// off would otherwise keep the box reserved forever.</summary>
    internal async Task ProcessMysteryBoxTimeoutsAsync(long nowMs, CancellationToken ct)
    {
        if (_state.MysteryBoxSessionsByOwnerId.Count == 0)
        {
            return;
        }

        List<RoomMysteryBoxSession>? expired = null;

        foreach (RoomMysteryBoxSession session in _state.MysteryBoxSessionsByOwnerId.Values)
        {
            if (nowMs - session.StartedAtMs < _roomConfig.MysteryBoxWaitTimeoutMs)
            {
                continue;
            }

            (expired ??= []).Add(session);
        }

        if (expired is null)
        {
            return;
        }

        foreach (RoomMysteryBoxSession session in expired)
        {
            await EndMysteryBoxSessionAsync(session, notifyParticipants: true).ConfigureAwait(true);
        }

        await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>Drops any pending open a leaving player was part of, so the box is not left reserved
    /// by someone who is no longer in the room.</summary>
    internal async Task CancelMysteryBoxSessionsForLeavingPlayerAsync(PlayerId playerId)
    {
        if (_state.MysteryBoxSessionsByOwnerId.Count == 0)
        {
            return;
        }

        List<RoomMysteryBoxSession>? affected = null;

        foreach (RoomMysteryBoxSession session in _state.MysteryBoxSessionsByOwnerId.Values)
        {
            if (session.IsParticipant(playerId))
            {
                (affected ??= []).Add(session);
            }
        }

        if (affected is null)
        {
            return;
        }

        foreach (RoomMysteryBoxSession session in affected)
        {
            if (session.BoxOwnerId == playerId)
            {
                await EndMysteryBoxSessionAsync(session, notifyParticipants: true)
                    .ConfigureAwait(true);

                continue;
            }

            session.KeyHolderId = null;

            if (session.IsAbandoned)
            {
                _state.MysteryBoxSessionsByOwnerId.Remove(session.BoxOwnerId);
            }
        }
    }

    /// <summary>Drops the pending open attached to a box that is being removed from the room.</summary>
    internal async Task CancelMysteryBoxSessionForItemAsync(RoomObjectId boxObjectId)
    {
        RoomMysteryBoxSession? attached = null;

        foreach (RoomMysteryBoxSession session in _state.MysteryBoxSessionsByOwnerId.Values)
        {
            if (session.BoxObjectId == boxObjectId)
            {
                attached = session;

                break;
            }
        }

        if (attached is null)
        {
            return;
        }

        await EndMysteryBoxSessionAsync(attached, notifyParticipants: true).ConfigureAwait(true);
    }

    private RoomMysteryBoxSession ResolveSession(IRoomItem box, PlayerId ownerId, string color)
    {
        if (
            _state.MysteryBoxSessionsByOwnerId.TryGetValue(
                ownerId,
                out RoomMysteryBoxSession? existing
            )
        )
        {
            // A session whose box has since left the room is stale; replace it rather than blocking
            // the owner's remaining boxes forever.
            if (_state.ItemsById.ContainsKey(existing.BoxObjectId))
            {
                return existing;
            }

            _state.MysteryBoxSessionsByOwnerId.Remove(ownerId);
        }

        RoomMysteryBoxSession session = new()
        {
            BoxObjectId = box.ObjectId,
            BoxOwnerId = ownerId,
            Color = color,
            StartedAtMs = NowMs(),
        };

        _state.MysteryBoxSessionsByOwnerId[ownerId] = session;

        return session;
    }

    private async Task OpenMysteryBoxAsync(
        ActionContext ctx,
        RoomMysteryBoxSession session,
        CancellationToken ct
    )
    {
        PlayerId keyHolderId = session.KeyHolderId!.Value;
        PlayerId ownerId = session.BoxOwnerId;

        if (!_state.ItemsById.TryGetValue(session.BoxObjectId, out IRoomItem? box))
        {
            await EndMysteryBoxSessionAsync(session, notifyParticipants: true).ConfigureAwait(true);

            return;
        }

        IPlayerMysteryBoxGrain keyHolderGrain = _grainFactory.GetPlayerMysteryBoxGrain(keyHolderId);

        // Spend the key before anything is handed out. The holder may have opened another box
        // between clicking and pairing, and a prize granted against a key that no longer exists is
        // free furniture.
        if (!await keyHolderGrain.TryConsumeKeyAsync(session.Color, ct).ConfigureAwait(true))
        {
            await EndMysteryBoxSessionAsync(session, notifyParticipants: true).ConfigureAwait(true);

            return;
        }

        // Claim the session before the box leaves the room: removing the furniture runs the
        // "a waiting box was picked up" cleanup, which would otherwise cancel this very open.
        _state.MysteryBoxSessionsByOwnerId.Remove(ownerId);

        // The box goes next, for the same reason the key did: while it still exists a second pairing
        // could claim it. If this fails the key is already gone, so refund it rather than eating it.
        if (!await ConsumeMysteryFurnitureAsync(ctx, box, ct).ConfigureAwait(true))
        {
            await keyHolderGrain
                .GrantKeyAsync(session.Color, "refund:box-open-failed", ct)
                .ConfigureAwait(true);
            await EndMysteryBoxSessionAsync(session, notifyParticipants: true).ConfigureAwait(true);

            return;
        }

        await _events
            .PublishAsync(
                new MysteryBoxOpenedEvent(
                    _state.RoomId.Value,
                    session.BoxObjectId.Value,
                    ownerId.Value,
                    keyHolderId.Value,
                    session.Color
                ),
                ct
            )
            .ConfigureAwait(true);

        // Two independent draws: the pair open the box together but do not split one prize.
        await Task.WhenAll(
                AwardMysteryBoxPrizeAsync(ownerId, session.Color, ct),
                AwardMysteryBoxPrizeAsync(keyHolderId, session.Color, ct)
            )
            .ConfigureAwait(true);

        // The owner just lost their box, so their tracker is stale. The key holder's was already
        // refreshed by the consume.
        await _grainFactory
            .GetPlayerMysteryBoxGrain(ownerId)
            .PushTrackerAsync(ct)
            .ConfigureAwait(true);
    }

    private async Task AwardMysteryBoxPrizeAsync(
        PlayerId playerId,
        string color,
        CancellationToken ct
    )
    {
        MysteryBoxPrizeSnapshot? prize = await _grainFactory
            .GetMysteryBoxManagerGrain()
            .PickPrizeAsync(MysteryBoxPrizePool.Box, color, ct)
            .ConfigureAwait(true);

        MysteryBoxPrizeAward? award = prize is null
            ? null
            : await _grainFactory
                .GetPlayerMysteryBoxGrain(playerId)
                .GrantPrizeAsync(prize, ct)
                .ConfigureAwait(true);

        if (award is null)
        {
            // Nothing could be awarded (empty pool, broken prize row). Close the dialog instead of
            // leaving the player staring at a spinner.
            _logger.LogWarning(
                "Mystery box opened in room {RoomId} but no prize could be awarded to player {PlayerId}.",
                _state.RoomId.Value,
                playerId.Value
            );

            await SendMysteryBoxComposerAsync(playerId, new CancelMysteryBoxWaitMessageComposer())
                .ConfigureAwait(true);

            return;
        }

        await _events
            .PublishAsync(
                new MysteryBoxPrizeAwardedEvent(
                    playerId.Value,
                    prize!.Id,
                    color,
                    award.ContentType,
                    award.ClassId
                ),
                ct
            )
            .ConfigureAwait(true);

        await SendMysteryBoxComposerAsync(
                playerId,
                new GotMysteryBoxPrizeMessageComposer
                {
                    ContentType = award.ContentType,
                    ClassId = award.ClassId,
                }
            )
            .ConfigureAwait(true);
    }

    /// <summary>Removes a mystery furniture from the room and the database. Returns false when the
    /// delete did not happen, so the caller can avoid handing out a prize for it.</summary>
    private async Task<bool> ConsumeMysteryFurnitureAsync(
        ActionContext ctx,
        IRoomItem item,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            FurnitureEntity entity = new() { Id = item.ObjectId.Value };
            dbCtx.Attach(entity);
            entity.DeletedAt = DateTime.UtcNow;
            dbCtx.Entry(entity).Property(f => f.DeletedAt).IsModified = true;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete mystery furniture {ObjectId} in room {RoomId}",
                item.ObjectId.Value,
                _state.RoomId.Value
            );

            return false;
        }

        await ObjectModule.RemoveObjectAsync(ctx, item, ct, item.OwnerId).ConfigureAwait(true);

        return true;
    }

    /// <summary>Repaints a box, tolerating the item having already left the room.</summary>
    private async Task SetMysteryBoxStateAsync(IRoomItem? box, int state)
    {
        if (box is not null && box.Logic.GetState() != state)
        {
            await box.Logic.SetStateAsync(state).ConfigureAwait(true);
        }
    }

    private async Task EndMysteryBoxSessionAsync(
        RoomMysteryBoxSession session,
        bool notifyParticipants
    )
    {
        _state.MysteryBoxSessionsByOwnerId.Remove(session.BoxOwnerId);

        // The box is no longer in play, so it goes back to its plain closed frame.
        _state.ItemsById.TryGetValue(session.BoxObjectId, out IRoomItem? box);
        await SetMysteryBoxStateAsync(box, MysteryBoxSprite.ClosedState(session.Color))
            .ConfigureAwait(true);

        if (!notifyParticipants)
        {
            return;
        }

        if (session.OwnerWaiting)
        {
            await SendMysteryBoxComposerAsync(
                    session.BoxOwnerId,
                    new CancelMysteryBoxWaitMessageComposer()
                )
                .ConfigureAwait(true);
        }

        if (session.KeyHolderId is PlayerId keyHolderId)
        {
            await SendMysteryBoxComposerAsync(
                    keyHolderId,
                    new CancelMysteryBoxWaitMessageComposer()
                )
                .ConfigureAwait(true);
        }
    }

    private Task SendMysteryBoxComposerAsync(
        PlayerId playerId,
        Primitives.Networking.IComposer composer
    ) => _grainFactory.GetPlayerPresenceGrain(playerId).SendComposerAsync(composer);
}
