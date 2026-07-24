using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Messages.Outgoing.Room.Chat;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Kicks the selected users out of the room (Habbo's "kick from room",
/// <c>wf_act_kick_user</c>, KickFromRoom.ts). The client sends a single string param — an optional kick
/// message (max 100 chars) — which is whispered to each user just before they are removed. Uses the
/// room grain's system/wired kick path (no human actor), called directly on the grain from inside its
/// own turn.</summary>
[RoomObjectLogic("wf_act_kick_user")]
public class WiredActionKickUser(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.KICK_FROM_ROOM;

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        string message = _wiredData.StringParam;

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            // Deliver the configured kick message before removing the user, so it reaches them while
            // they are still in the room. (Whispered via the presence grain, per the composer-routing
            // rule; the pixel-perfect kick notification would need the revision's notification composer,
            // which is a bare stub today.)
            if (
                !string.IsNullOrWhiteSpace(message)
                && _roomGrain._state.AvatarsByPlayerId.TryGetValue(
                    playerId,
                    out RoomObjectId objectId
                )
            )
            {
                await _grainFactory
                    .GetPlayerPresenceGrain(playerId)
                    .SendComposerAsync(
                        new WhisperMessageComposer
                        {
                            ObjectId = objectId,
                            Text = message,
                            Gesture = default,
                            StyleId = 0,
                            Links = [],
                            TrackingId = 0,
                        }
                    );
            }

            await _roomGrain.KickUserFromWiredAsync(playerId, ct);
        }

        return true;
    }
}
