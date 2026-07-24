using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Emits a wired signal (client SendSignal.ts). The action's selected furni and users are handed to
/// every receive-signal trigger in the room, which reads them through the SignalItems / SignalUsers
/// sources. Int params <c>[split_furni, split_users]</c> control batching: by default one signal carries
/// the whole selection; "split furni" sends one signal per furni and "split users" one per user, so a
/// receive-signal stack fires once per item instead of once for the batch.
/// </summary>
[RoomObjectLogic("wf_act_send_signal")]
public class WiredActionSendSignal(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    // A hard cap on how many signals one activation may fan out to, so a large selection with both
    // split options set (a furni x user cross product) cannot flood the event queue.
    private const int MaxSignalsPerActivation = 128;

    public override int WiredCode => (int)WiredActionType.SEND_SIGNAL;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(false), // split furni: one signal per furni
            new WiredBoolParamRule(false), // split users: one signal per user
        ];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [WiredPlayerSourceType.TriggeredUser, WiredPlayerSourceType.SelectorUsers],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        bool splitFurni = _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<bool>(0);
        bool splitUsers = _wiredData.IntParams.Count > 1 && _wiredData.GetIntParam<bool>(1);

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        List<int> furni = [.. selection.SelectedFurniIds];
        List<int> users = [.. selection.SelectedPlayerIds];

        // One "group" per emitted signal. A split source becomes one group per member; an unsplit one
        // becomes a single group with the whole set (empty groups are still one empty group, so a bare
        // signal with no payload still fires its receive-signal stacks).
        IReadOnlyList<int>[] furniGroups = splitFurni
            ? [.. furni.Select(f => (IReadOnlyList<int>)[f])]
            : [furni];
        IReadOnlyList<int>[] userGroups = splitUsers
            ? [.. users.Select(u => (IReadOnlyList<int>)[u])]
            : [users];

        int emitted = 0;

        foreach (IReadOnlyList<int> furniGroup in furniGroups)
        {
            foreach (IReadOnlyList<int> userGroup in userGroups)
            {
                if (emitted++ >= MaxSignalsPerActivation)
                {
                    return true;
                }

                await _roomGrain.PublishRoomEventAsync(
                    new SignalRoomEvent
                    {
                        RoomId = _roomGrain.RoomId,
                        CausedBy = ActionContext.CreateForWired(_roomGrain.RoomId),
                        FurniIds = furniGroup,
                        PlayerIds = userGroup,
                    },
                    ct
                );
            }
        }

        return true;
    }
}
