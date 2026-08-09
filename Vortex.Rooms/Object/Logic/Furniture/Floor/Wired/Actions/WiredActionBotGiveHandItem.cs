using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Habbo's "give hand item" wired: the resolved users end up holding something.
/// <para>
/// Its form has a checkbox for whether a bot does the handing, and the string param carries that
/// bot's name only when the box is ticked — an empty one means the wired hands the item over
/// itself. The hand item id is int param [0].
/// </para>
/// <para>
/// The named bot is asked to walk over first, because a bot handing a drink across the room reads
/// as a bug; the item is given either way, since making it wait on the walk would have a stack that
/// fires once give nothing at all.
/// </para>
/// </summary>
[RoomObjectLogic("wf_act_bot_give_handitem")]
public class WiredActionBotGiveHandItem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.BOT_GIVE_HAND_ITEM;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            // The client's dropdown is a list of hand item ids; anything outside its range would be
            // an item the client cannot draw.
            new WiredRangeParamRule(0, 9999, 0),
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
        int handItemId = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : 0;

        if (handItemId <= 0)
        {
            return true;
        }

        string botName = (_wiredData.StringParam ?? string.Empty).Trim();

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            if (botName.Length > 0)
            {
                await ctx.ProcessBotWalkToPlayerAsync(botName, new PlayerId(playerId));
            }

            await ctx.ProcessGiveHandItemAsync(new PlayerId(playerId), handItemId);
        }

        return true;
    }
}
