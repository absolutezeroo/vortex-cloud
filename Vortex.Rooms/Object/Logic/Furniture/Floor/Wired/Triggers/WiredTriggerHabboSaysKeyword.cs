using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_says_something")]
public class WiredTriggerHabboSaysKeyword(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.AVATAR_SAYS_SOMETHING;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PlayerChatEvent)];

    // intParams layout mirrors the client form (AvatarSaysSomething.readIntParamsFromForm):
    // [0] onlyOwner (bool), [1] matchType (0/1/2), [2] hideMessage (bool).
    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(false),
            new WiredRangeParamRule(0, 2, 0),
            new WiredBoolParamRule(false),
        ];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not PlayerChatEvent evt)
        {
            return Task.FromResult(false);
        }

        bool onlyOwner = _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<bool>(0);

        if (onlyOwner && evt.PlayerId != _roomGrain._state.RoomSnapshot.OwnerId)
        {
            return Task.FromResult(false);
        }

        int matchType =
            _wiredData.IntParams.Count > 1
                ? _wiredData.GetIntParam<int>(1)
                : WiredChatKeywordMatcher.MatchContains;

        if (!WiredChatKeywordMatcher.Matches(evt.Message, _wiredData.StringParam, matchType))
        {
            return Task.FromResult(false);
        }

        // The speaker is the triggering user for downstream "triggered user" effects.
        ctx.Selected.SelectedPlayerIds.Add(evt.PlayerId);

        // NOTE: the hideMessage flag (intParams[2]) is not honoured yet — suppressing the spoken
        // line would require the chat path to consult wired synchronously before broadcasting.

        return Task.FromResult(true);
    }
}
