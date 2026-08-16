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
/// "WIRED Effect: Execute Stacks" — runs other piles from this one. The furni's own description is
/// the specification: it executes the selected stacks "regardless of existing conditions or
/// triggers".
/// </summary>
/// <remarks>
/// This is the wired language's composition primitive: without it a pile can only be reached by its
/// own trigger, so shared behaviour has to be copied into every pile that needs it.
/// <para>
/// The box carries no form of its own — the client class declares a code and nothing else — so the
/// configuration is purely which furni it points at, and each of their tiles is a pile to run. The
/// selection travels with the call, so a pile called from a walk-on trigger still acts on the user
/// who walked on.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_act_call_stacks")]
public class WiredActionCallStacks(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.CALL_ANOTHER_STACK;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        if (selection.SelectedFurniIds.Count == 0)
        {
            return true;
        }

        // The room owns the recursion guard: this box's own tile goes into the call chain for the
        // duration, so a pile pointed at itself does not run itself.
        await _ctx.Furni.ExecuteWiredStacksAtAsync(
            _ctx.GetTileIdx(),
            selection.SelectedFurniIds,
            ctx.Selected,
            ct
        );

        return true;
    }
}
