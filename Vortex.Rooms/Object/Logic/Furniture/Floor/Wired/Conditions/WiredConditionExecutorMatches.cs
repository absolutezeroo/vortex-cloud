using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// Passes when whoever set the trigger off is of the configured kind (Habbo's "triggerer matches",
/// TriggererMatches.ts). Int param [0] is the entity type, and the string param, when non-empty,
/// further restricts it to an avatar of that exact name. The negative variant inherits this and flips
/// <see cref="FurnitureWiredConditionLogic.IsNegative"/>.
/// <para>
/// The client's type ids (loc <c>wiredfurni.params.usertype.*</c>: 1 Habbo, 2 Pet, 4 Bot) are the same
/// numbers as <see cref="RoomObjectType"/>, so the parameter compares directly against the avatar's
/// own type. Note the room only routes players as triggerers today, so the Pet and Bot choices cannot
/// match yet — they will start working on their own once those entities can fire triggers.
/// </para>
/// </summary>
[RoomObjectLogic("wf_cnd_triggerer_match")]
public class WiredConditionExecutorMatches(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.TRIGGERER_MATCHES;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredParamRule((int)RoomObjectType.Player)];

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        int requiredType =
            _wiredData.IntParams.Count > 0
                ? _wiredData.GetIntParam<int>(0)
                : (int)RoomObjectType.Player;

        string requiredName = _wiredData.StringParam;
        PlayerId triggerer = ctx.Event.CausedBy.PlayerId;

        bool result =
            triggerer > 0
            && TryGetTriggerer(triggerer, out IRoomAvatar? avatar)
            && (int)avatar.AvatarType == requiredType
            && (
                string.IsNullOrEmpty(requiredName)
                || string.Equals(avatar.Name, requiredName, StringComparison.OrdinalIgnoreCase)
            );

        return IsNegative() ? !result : result;
    }

    private bool TryGetTriggerer(PlayerId playerId, [NotNullWhen(true)] out IRoomAvatar? avatar)
    {
        avatar = null;

        return _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
    }
}
