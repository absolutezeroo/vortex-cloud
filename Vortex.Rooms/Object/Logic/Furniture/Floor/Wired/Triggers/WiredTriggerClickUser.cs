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
    // presentation hints (suppress the context menu / don't rotate the clicker) that would need a
    // synchronous reply to the click; they are persisted here but not yet honoured server-side.
    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredBoolParamRule(false), new WiredBoolParamRule(false)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Event is PlayerClickedPlayerEvent);
}
