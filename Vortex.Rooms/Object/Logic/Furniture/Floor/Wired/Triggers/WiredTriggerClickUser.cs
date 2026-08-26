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
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

[RoomObjectLogic("wf_trg_click_user")]
public class WiredTriggerClickUser(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.AVATAR_CLICKS_AVATAR;
    public override List<Type> SupportedEventTypes { get; } = [typeof(PlayerClickedPlayerEvent)];

    // Client (UserClicksUser.ts): intParams = [blockMenuOpen, doNotRotate]. Both are client-side
    // presentation hints. blockMenuOpen is answered on the WiredClickUser round trip below;
    // doNotRotate belongs to the look-at the client sends separately and is still not honoured.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredBoolParamRule(false), new WiredBoolParamRule(false)];

    /// <summary>
    /// intParams[0] — whether the clicker's context menu must stay shut.
    /// </summary>
    /// <remarks>
    /// Read after <c>LoadWiredAsync</c>, which the trigger index does when it hydrates the box. An
    /// unconfigured box reports false, which is the client's own default: the menu opens.
    /// </remarks>
    public bool BlocksMenuOpen => _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<bool>(0);

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is PlayerClickedPlayerEvent);
}
