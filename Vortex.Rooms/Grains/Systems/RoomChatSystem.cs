using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Grains.Systems;

public sealed class RoomChatSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;
    private static readonly int MaxChatMessageLength = 100;

    public async Task SendChatFromPlayerAsync(
        PlayerId playerId,
        string text,
        AvatarGestureType gesture,
        int styleId,
        List<(string, string, bool)> links,
        int trackingId,
        PlayerId? targetPlayerId = null
    )
    {
        if (
            targetPlayerId is not null
            && !_roomGrain._state.AvatarsByPlayerId.ContainsKey(targetPlayerId.Value)
        )
        {
            return;
        }

        if (
            !_roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
        )
        {
            return;
        }

        if (IsUserMuted(playerId, out int secondsRemaining))
        {
            await _roomGrain
                ._grainFactory.GetPlayerPresenceGrain(playerId)
                .SendComposerAsync(
                    new RemainingMutePeriodMessageComposer { SecondsRemaining = secondsRemaining }
                )
                .ConfigureAwait(false);

            return;
        }

        if (styleId == -1)
        {
            styleId = avatar.LastChatStyleId;
        }
        else
        {
            avatar.LastChatStyleId = styleId;
        }

        await SendChatAsync(
                avatar.ObjectId,
                playerId,
                text,
                gesture,
                styleId,
                links,
                trackingId,
                targetPlayerId
            )
            .ConfigureAwait(false);
    }

    private async Task SendChatAsync(
        RoomObjectId objectId,
        PlayerId playerId,
        string text,
        AvatarGestureType gesture,
        int styleId,
        List<(string, string, bool)> links,
        int trackingId,
        PlayerId? targetPlayerId
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (targetPlayerId is null)
        {
            await _roomGrain
                .SendComposerToRoomAsync(
                    new ChatMessageComposer
                    {
                        ObjectId = objectId,
                        Text = text,
                        Gesture = gesture,
                        StyleId = styleId,
                        Links = links,
                        TrackingId = trackingId,
                    }
                )
                .ConfigureAwait(false);
        }
        else
        {
            WhisperMessageComposer whisperComposer = new WhisperMessageComposer
            {
                ObjectId = objectId,
                Text = text,
                Gesture = gesture,
                StyleId = styleId,
                Links = links,
                TrackingId = trackingId,
            };

            await Task.WhenAll(
                    _roomGrain
                        ._grainFactory.GetPlayerPresenceGrain(playerId)
                        .SendComposerAsync(whisperComposer),
                    _roomGrain
                        ._grainFactory.GetPlayerPresenceGrain(targetPlayerId.Value)
                        .SendComposerAsync(whisperComposer)
                )
                .ConfigureAwait(false);
        }

        await PersistChatAsync(playerId, targetPlayerId, text).ConfigureAwait(false);
    }

    private async Task PersistChatAsync(PlayerId playerId, PlayerId? targetPlayerId, string text)
    {
        try
        {
            await using TurboDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync()
                .ConfigureAwait(false);

            dbCtx.Chatlogs.Add(
                new RoomChatlogEntity
                {
                    RoomEntityId = _roomGrain.RoomId,
                    PlayerEntityId = playerId,
                    TargetPlayerEntityId = targetPlayerId,
                    Message =
                        text.Length > MaxChatMessageLength ? text[..MaxChatMessageLength] : text,
                }
            );

            await dbCtx.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to persist room chat log for room {RoomId}.",
                _roomGrain.RoomId
            );
        }
    }

    private bool IsUserMuted(PlayerId playerId, out int secondsRemaining)
    {
        if (
            _roomGrain._state.MuteExpiresUtc.TryGetValue(playerId, out DateTime mutedUntil)
            && mutedUntil > DateTime.UtcNow
        )
        {
            secondsRemaining = (int)Math.Ceiling((mutedUntil - DateTime.UtcNow).TotalSeconds);
            return true;
        }

        _roomGrain._state.MuteExpiresUtc.Remove(playerId);
        secondsRemaining = 0;
        return false;
    }
}
