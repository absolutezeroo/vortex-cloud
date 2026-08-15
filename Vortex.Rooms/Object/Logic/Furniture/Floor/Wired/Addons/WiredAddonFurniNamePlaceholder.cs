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

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>"WIRED Text Add-on: Furni name placeholder" — <c>$(name)</c> becomes the name of the
/// furni the pile is acting on, as the catalogue calls it.</summary>
[RoomObjectLogic("wf_xtra_text_output_furni_name")]
public class WiredAddonFurniNamePlaceholder(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredAddonTextPlaceholder(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.FURNI_NAME_PLACEHOLDER;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    protected override async Task<IReadOnlyList<string>> ResolveValuesAsync(
        IWiredExecutionContext ctx,
        CancellationToken ct
    )
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        List<string> names = [];

        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (_ctx.Lookup.TryFindItem(furniId, out IRoomItem? item))
            {
                names.Add(item.Definition.Name);
            }
        }

        return names;
    }
}
