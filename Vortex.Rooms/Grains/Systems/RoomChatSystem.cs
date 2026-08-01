using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;

namespace Vortex.Rooms.Grains.Systems;

public sealed class RoomChatSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;
    private static readonly int MaxChatMessageLength = 100;

    // Per-player last-accepted-line timestamp, for the room's configured flood sensitivity
    // (SEC-03). Lives only for this room activation; bounded by how many distinct players have
    // spoken in the room since it last activated.
    private readonly Dictionary<PlayerId, long> _lastChatAtMs = new();

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

        if (IsFloodGated(playerId, out int floodSecondsRemaining))
        {
            await _roomGrain
                ._grainFactory.GetPlayerPresenceGrain(playerId)
                .SendComposerAsync(
                    new FloodControlMessageComposer { Seconds = floodSecondsRemaining }
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

        // A public line may also be an order to one of the speaker's pets.
        if (targetPlayerId is null)
        {
            await TryIssuePetCommandFromChatAsync(playerId, text).ConfigureAwait(false);
        }

        // Public chat (not whispers) feeds the wired "avatar says (keyword)" trigger.
        if (targetPlayerId is null && !string.IsNullOrWhiteSpace(text))
        {
            await _roomGrain
                .PublishRoomEventAsync(
                    new PlayerChatEvent
                    {
                        RoomId = _roomGrain.RoomId,
                        CausedBy = ActionContext.CreateForPlayer(playerId, _roomGrain.RoomId),
                        PlayerId = playerId,
                        Message = text,
                    },
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Recognises "&lt;pet name&gt; &lt;command&gt;" in a public line and runs the command.
    ///
    /// There is no "issue pet command" packet in the client: AS3's PetCommandTool builds the string
    /// "&lt;pet name&gt; &lt;localised command&gt;" and hands it to roomSession.sendChatMessage(), so
    /// an order and a chat line are the same message on the wire. That is also why a player can type
    /// the order by hand. Only the pet's own owner is obeyed, which IssueCommandAsync enforces again.
    /// </summary>
    private async Task TryIssuePetCommandFromChatAsync(PlayerId playerId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _roomGrain._state.PetsById.Count == 0)
        {
            return;
        }

        string trimmed = text.Trim();
        int split = trimmed.IndexOf(' ');

        // A bare name with no command word is just chat.
        if (split <= 0)
        {
            return;
        }

        string spokenName = trimmed[..split];
        string spokenCommand = trimmed[(split + 1)..].Trim();

        if (spokenCommand.Length == 0)
        {
            return;
        }

        foreach (PetSnapshot pet in _roomGrain._state.PetsById.Values)
        {
            if (pet.OwnerId != playerId)
            {
                continue;
            }

            if (!string.Equals(pet.Name, spokenName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int? commandId = _roomGrain._petCommandProvider.ResolveCommandIdByName(
                pet.Type,
                spokenCommand
            );

            if (commandId is null)
            {
                return;
            }

            await _roomGrain
                .PetSystem.IssueCommandAsync(
                    ActionContext.CreateForPlayer(playerId, _roomGrain.RoomId),
                    pet.PetId,
                    commandId.Value,
                    CancellationToken.None
                )
                .ConfigureAwait(false);

            return;
        }
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
            await using VortexDbContext dbCtx = await _roomGrain
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

    private bool IsFloodGated(PlayerId playerId, out int secondsRemaining)
    {
        int[] intervals = _roomGrain._roomConfig.ChatFloodIntervalSeconds;
        int sensitivityIndex = (int)_roomGrain._state.RoomSnapshot.ChatSettings.FloodSensitivity;
        int intervalSeconds =
            intervals.Length == 0 ? 0
            : sensitivityIndex >= 0 && sensitivityIndex < intervals.Length
                ? intervals[sensitivityIndex]
            : intervals[^1];

        long nowMs = Environment.TickCount64;

        if (intervalSeconds > 0 && _lastChatAtMs.TryGetValue(playerId, out long lastMs))
        {
            long requiredMs = intervalSeconds * 1000L;
            long elapsedMs = nowMs - lastMs;

            if (elapsedMs < requiredMs)
            {
                secondsRemaining = (int)Math.Ceiling((requiredMs - elapsedMs) / 1000.0);
                return true;
            }
        }

        _lastChatAtMs[playerId] = nowMs;
        secondsRemaining = 0;
        return false;
    }
}
