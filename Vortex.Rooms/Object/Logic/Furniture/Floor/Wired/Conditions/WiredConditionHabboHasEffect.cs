using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>Passes when the triggering player is wearing the configured effect (Habbo's "actor is wearing
/// effect"). Client ActorIsWearingEffect stores a single number input as intParams [effectId], so this
/// compares the actor's currently worn effect id against that value.</summary>
[RoomObjectLogic("wf_cnd_wearing_effect")]
public class WiredConditionHabboHasEffect(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.ACTOR_IS_WEARING_EFFECT;

    // [0] = the effect id to match. Param rules must be declared or the client config update is rejected
    // by the normalizer (FurnitureWiredLogic.TryNormalizeIntParams).
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredParamRule(0)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;
        int effectId = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;
        bool result = false;

        if (
            triggerer > 0
            && _roomGrain._state.AvatarsByPlayerId.TryGetValue(triggerer, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
        )
        {
            result = avatar.CurrentEffectId == effectId;
        }

        return IsNegative() ? !result : result;
    }
}
