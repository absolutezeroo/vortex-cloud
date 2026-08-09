using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Habbo's "move bot" wired: the named bot walks to the selected furni rather than appearing on it.
/// The walk is a standing order the room tick works through a tile at a time, which is what lets the
/// bot-reaches-furni trigger fire on the way.
/// </summary>
[RoomObjectLogic("wf_act_bot_move")]
public class WiredActionBotMove(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.BOT_MOVE;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        string botName = (_wiredData.StringParam ?? string.Empty).Trim();

        if (botName.Length == 0)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        if (!TryResolveDestinationTile(selection, out int tileIdx))
        {
            return true;
        }

        await ctx.ProcessBotMovementAsync(botName, tileIdx, instant: false);

        return true;
    }
}
