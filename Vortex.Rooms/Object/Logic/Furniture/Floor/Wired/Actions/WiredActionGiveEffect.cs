using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Applies an avatar effect to the selected users (Habbo's "give effect",
/// <c>wf_act_give_effect</c>). Client GiveEffect.ts: intParams = [effectId, priority, type]. The
/// effect is broadcast to the room; priority/type (permanent-vs-timed nuance) are persisted but not
/// yet honoured.</summary>
[RoomObjectLogic("wf_act_give_effect")]
public class WiredActionGiveEffect(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.GIVE_EFFECT;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 10000, 0),
            new WiredRangeParamRule(0, 10000, 0),
            new WiredBoolParamRule(false),
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
        int effectId = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;

        if (effectId <= 0)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            if (
                _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            )
            {
                await ctx.SendComposerToRoomAsync(
                    new AvatarEffectMessageComposer
                    {
                        UserId = objectId,
                        EffectId = effectId,
                        DelayMilliseconds = 0,
                    }
                );
            }
        }

        return true;
    }
}
