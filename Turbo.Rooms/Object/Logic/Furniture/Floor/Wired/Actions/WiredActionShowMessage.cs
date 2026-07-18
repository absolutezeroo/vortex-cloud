using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Messages.Outgoing.Room.Chat;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Shows a chat bubble over each resolved user (Habbo's "show message" wired). The string
/// param is the message text; int params from the client setup form (actiontypes/class_3972): [0] =
/// visibility, [1] = chat-bubble style id (one of the wired notification styles 34 / 200-252). The
/// bubble is emitted as normal room chat — it is NOT mute-checked and NOT written to the chat log,
/// since it is not the user typing. Visibility 1 = the whole room sees the bubble (ChatMessageComposer);
/// 0 = only the user sees it over their own head (WhisperMessageComposer to that player only). The two
/// furnidata variants (wf_act_show_message / wf_act_show_message_room) share this one action code and
/// both read the visibility from param [0] — the mapping is grounded in the "_room" key name; verify
/// the exact id direction against a live setup if it appears inverted.</summary>
[RoomObjectLogic("wf_act_show_message")]
public class WiredActionShowMessage(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int VisibilityRoom = 1;

    public override int WiredCode => (int)WiredActionType.CHAT;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 0), // visibility: 0 = triggerer only, 1 = whole room
            new WiredRangeParamRule(0, 255, 0), // chat-bubble style id
        ];

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
        string text = _wiredData.StringParam;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        int visibility = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        int styleId = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : 0;

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            try
            {
                if (
                    !_roomGrain._state.AvatarsByPlayerId.TryGetValue(
                        playerId,
                        out RoomObjectId objectId
                    )
                )
                {
                    continue;
                }

                if (visibility == VisibilityRoom)
                {
                    await ctx.SendComposerToRoomAsync(
                        new ChatMessageComposer
                        {
                            ObjectId = objectId,
                            Text = text,
                            Gesture = default,
                            StyleId = styleId,
                            Links = [],
                            TrackingId = 0,
                        }
                    );
                }
                else
                {
                    await _grainFactory
                        .GetPlayerPresenceGrain(playerId)
                        .SendComposerAsync(
                            new WhisperMessageComposer
                            {
                                ObjectId = objectId,
                                Text = text,
                                Gesture = default,
                                StyleId = styleId,
                                Links = [],
                                TrackingId = 0,
                            }
                        );
                }
            }
            catch
            {
                continue;
            }
        }

        return true;
    }
}
