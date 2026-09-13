using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// The same comparison as <see cref="WiredConditionChestHasItems"/>, counting only the item types
/// the box was shown.
/// </summary>
/// <remarks>
/// The client builds this box by subclassing the plain one and moving the slots along: its own form
/// (conditions/chests, code 46) keeps every int param and every handler, and changes only which
/// furni slot means what. Slot 0 becomes the item types, slot 1 becomes the chests, and the merged
/// amount source moves to slot 2 — <c>mergedSelections()</c> returns <c>[2, 0]</c>. Three furni
/// slots have to be declared for the picker to resolve that without reading past the end of the
/// list.
/// <para>
/// A type is named by example: the builder drops a furni of the kind they mean into the first slot
/// and the chest is counted for items like it.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_chest_has_item_type")]
public class WiredConditionChestHasItemTypes(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredConditionChestHasItems(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredConditionType.CHEST_HAS_ITEM_TYPES;

    /// <inheritdoc/>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [.. FurniSources],
            [.. FurniSources],
            [.. FurniSources],
        ];

    /// <summary>Slot 1 here, because slot 0 holds the item types.</summary>
    protected override List<int> GetChestIds() => GetStuffIds2();

    /// <inheritdoc/>
    protected override List<int> GetKindExampleIds() => GetStuffIds();
}
