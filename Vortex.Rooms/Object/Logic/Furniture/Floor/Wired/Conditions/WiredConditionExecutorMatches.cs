using System;
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

/// <summary>Passes when the triggering user matches the configured criteria (Habbo's "triggerer
/// matches", TriggererMatches.ts): int param [0] = user type (1 = room owner, 2 = has rights, 4 = any),
/// and the string param, when non-empty, restricts it to an avatar of that exact name. The negative
/// variant inherits this and flips <see cref="FurnitureWiredConditionLogic.IsNegative"/>.</summary>
[RoomObjectLogic("wf_cnd_triggerer_match")]
public class WiredConditionExecutorMatches(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int UserTypeOwner = 1;
    private const int UserTypeRights = 2;
    private const int UserTypeAny = 4;

    public override int WiredCode => (int)WiredConditionType.TRIGGERER_MATCHES;

    public override List<IWiredParamRule> GetIntParamRules() => [new WiredParamRule(UserTypeAny)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        int userType =
            _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : UserTypeAny;
        string requiredName = _wiredData.StringParam;
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;

        bool result;

        if (triggerer <= 0)
        {
            result = false;
        }
        else
        {
            PlayerId owner = _roomGrain._state.RoomSnapshot.OwnerId;

            result = userType switch
            {
                UserTypeOwner => triggerer == owner,
                UserTypeRights => triggerer == owner
                    || _roomGrain._state.PlayerIdsWithRights.Contains(triggerer),
                _ => true,
            };

            if (result && !string.IsNullOrEmpty(requiredName))
            {
                result =
                    TryGetTriggererName(triggerer, out string? name)
                    && string.Equals(name, requiredName, StringComparison.OrdinalIgnoreCase);
            }
        }

        return IsNegative() ? !result : result;
    }

    private bool TryGetTriggererName(PlayerId playerId, out string? name)
    {
        if (
            _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
        )
        {
            name = avatar.Name;
            return true;
        }

        name = null;
        return false;
    }
}
