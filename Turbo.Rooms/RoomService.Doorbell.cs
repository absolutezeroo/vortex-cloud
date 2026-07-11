using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Messages.Outgoing.Room.Session;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.Rooms;

internal sealed partial class RoomService
{
    public async Task AnswerDoorbellAsync(
        ActionContext actorCtx,
        PlayerId targetPlayerId,
        bool admit,
        CancellationToken ct
    )
    {
        if (actorCtx.PlayerId <= 0 || targetPlayerId <= 0 || actorCtx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain room = _grainFactory.GetRoomGrain(actorCtx.RoomId);
        RoomControllerType controllerLevel = await room.GetControllerLevelAsync(actorCtx, ct)
            .ConfigureAwait(false);

        if (controllerLevel < RoomControllerType.Rights)
        {
            return;
        }

        bool wasRinging = await room.TryRemoveDoorbellRingAsync(targetPlayerId, ct)
            .ConfigureAwait(false);

        if (!wasRinging)
        {
            return;
        }

        string targetName = await _grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerNameAsync(targetPlayerId, ct)
            .ConfigureAwait(false);
        ImmutableArray<PlayerId> notifyTargets = await room.GetPresentRightsHoldersAsync()
            .ConfigureAwait(false);
        IComposer occupantComposer = admit
            ? new FlatAccessibleMessageComposer { RoomId = actorCtx.RoomId, Username = targetName }
            : new FlatAccessDeniedMessageComposer
            {
                RoomId = actorCtx.RoomId,
                Username = targetName,
            };

        await Task.WhenAll(
                notifyTargets.Select(id =>
                    _grainFactory.GetPlayerPresenceGrain(id).SendComposerAsync(occupantComposer)
                )
            )
            .ConfigureAwait(false);

        IPlayerPresenceGrain targetPresence = _grainFactory.GetPlayerPresenceGrain(targetPlayerId);

        if (!admit)
        {
            await targetPresence
                .SendComposerAsync(
                    new FlatAccessDeniedMessageComposer
                    {
                        RoomId = actorCtx.RoomId,
                        Username = string.Empty,
                    }
                )
                .ConfigureAwait(false);
            await targetPresence.SetPendingRoomAsync(RoomId.Invalid, false).ConfigureAwait(false);

            return;
        }

        await targetPresence
            .SendComposerAsync(
                new FlatAccessibleMessageComposer
                {
                    RoomId = actorCtx.RoomId,
                    Username = string.Empty,
                }
            )
            .ConfigureAwait(false);

        ActionContext targetCtx = ActionContext.CreateForPlayer(targetPlayerId, actorCtx.RoomId);

        await CompleteRoomEntryAsync(targetCtx, targetPlayerId, actorCtx.RoomId, ct)
            .ConfigureAwait(false);
    }
}
