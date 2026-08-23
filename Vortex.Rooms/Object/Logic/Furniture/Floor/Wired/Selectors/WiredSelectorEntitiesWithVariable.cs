using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_users_with_var")]
public class WiredSelectorEntitiesWithVariable(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredSelectorType.USERS_WITH_VARIABLE;

    public override List<IWiredParamRule> GetIntParamRules() => [new WiredBoolParamRule(false)];

    /// <summary>
    /// The pool this selector reads is one merged input source, and the client addresses it as
    /// slot 0 of both lists (<c>mergedSelections()</c> returns <c>[[0, 0]]</c>). Only the furni
    /// half was declared, so the arrow on a selector whose whole subject is users read past the
    /// end of the player list -- unchecked on both sides of the port.
    /// </summary>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    /// <inheritdoc cref="GetAllowedFurniSources"/>
    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        IWiredSelectionSet input = await ctx.GetWiredSelectionSetAsync(this, ct);
        List<int> allowedDefinitionIds = new List<int>();
        WiredSelectionSet output = new WiredSelectionSet();

        foreach (int id in input.SelectedFurniIds)
        {
            try
            {
                if (!_ctx.Lookup.TryFindItem(id, out IRoomItem? item))
                {
                    continue;
                }

                allowedDefinitionIds.Add(item.Definition.Id);
            }
            catch
            {
                continue;
            }
        }

        foreach (IRoomItem item in _ctx.Lookup.Items)
        {
            if (allowedDefinitionIds.Contains(item.Definition.Id))
            {
                output.SelectedFurniIds.Add((int)item.ObjectId);
            }
        }

        return output;
    }
}
