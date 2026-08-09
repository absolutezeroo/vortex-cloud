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
/// Habbo's "teleport bot" wired: the named bot appears on the selected furni's tile without walking.
/// Its setup form asks for nothing but the name — the destination is whatever furni the stack has
/// selected, the same as the teleport-user wired.
/// </summary>
[RoomObjectLogic("wf_act_bot_teleport")]
public class WiredActionBotTeleport(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.BOT_TELEPORT;

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

        await ctx.ProcessBotMovementAsync(botName, tileIdx, instant: true);

        return true;
    }
}
