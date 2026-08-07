using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Object.Logic.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Wall;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Furniture whose interesting state is data rather than a state number: a mannequin's outfit, a
/// sticky note's text. All of it goes through the same three steps — find the item, check the actor
/// may build here, write and persist — so they are kept together rather than spread across the
/// individual widget handlers.
/// </summary>
public sealed partial class RoomGrain
{
    private const string MannequinGenderKey = "GENDER";
    private const string MannequinFigureKey = "FIGURE";
    private const string MannequinNameKey = "OUTFIT_NAME";

    /// <summary>
    /// Resolves an item the actor is allowed to reconfigure. Rights, not ownership: a room owner
    /// expects to be able to fix a mannequin a visitor left in their room, and Habbo has always
    /// worked that way.
    /// </summary>
    private async Task<IRoomItem?> FindManipulableItemAsync(ActionContext ctx, RoomObjectId itemId)
    {
        if (!_state.ItemsById.TryGetValue(itemId, out IRoomItem? item))
        {
            return null;
        }

        return await SecurityModule.CanManipulateFurniAsync(ctx).ConfigureAwait(true) ? item : null;
    }

    public async Task<bool> SetMannequinOutfitAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        string figure,
        string gender,
        CancellationToken ct
    )
    {
        IRoomItem? item = await FindManipulableItemAsync(ctx, itemId).ConfigureAwait(true);

        if (item?.Logic.StuffData is not IMapStuffData map || string.IsNullOrWhiteSpace(figure))
        {
            return false;
        }

        map.Data[MannequinFigureKey] = figure;
        map.Data[MannequinGenderKey] = gender;

        // A mannequin that has never been named keeps whatever the client last showed, so the key
        // is created empty rather than left absent — the widget reads it unconditionally.
        _ = map.Data.TryAdd(MannequinNameKey, string.Empty);

        await item.Logic.PersistStuffDataAsync().ConfigureAwait(true);

        return true;
    }

    public async Task<bool> SetMannequinNameAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        string name,
        CancellationToken ct
    )
    {
        IRoomItem? item = await FindManipulableItemAsync(ctx, itemId).ConfigureAwait(true);

        if (item?.Logic.StuffData is not IMapStuffData map)
        {
            return false;
        }

        map.Data[MannequinNameKey] = name;

        await item.Logic.PersistStuffDataAsync().ConfigureAwait(true);

        return true;
    }

    /// <summary>
    /// The widget's "back to normal" button, and the only value it sends that is not a height.
    /// </summary>
    private const int ResetStackHeightSentinel = -100;

    public async Task<bool> SetCustomStackHeightAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int heightHundredths,
        bool? multiWalk,
        CancellationToken ct
    )
    {
        IRoomItem? item = await FindManipulableItemAsync(ctx, itemId).ConfigureAwait(true);

        // Gated on the logic, not on the packet: any client can name any object id here, and moving
        // an arbitrary furni to an arbitrary altitude is exactly what the magic tile is allowed to
        // do and nothing else is.
        if (
            item is not IRoomFloorItem floor
            || floor.Logic is not FurnitureCustomStackHeightLogic logic
        )
        {
            return false;
        }

        // Sent on its own when the checkbox is what moved, so it is applied before the height is
        // range-checked — otherwise ticking the box at an out-of-range height would silently drop it.
        if (multiWalk is not null)
        {
            logic.SetMultiWalk(multiWalk.Value);
        }

        bool reset = heightHundredths == ResetStackHeightSentinel;

        if (
            !reset
            && (heightHundredths < 0 || heightHundredths > _roomConfig.MaxStackHeight.ToInt())
        )
        {
            return false;
        }

        int tileIdx = MapModule.ToIdx(floor.X, floor.Y);

        if (!MapModule.InBounds(tileIdx))
        {
            return false;
        }

        if (reset)
        {
            // Taken out of the stack first so the tile height recomputes without it — asking for
            // the tile's current height while the tile is still the thing defining it would just
            // hand back the height being cleared.
            MapModule.RemoveFloorItem(floor);
            MapModule.PlaceFloorItem(floor, tileIdx, floor.Rotation);
        }
        else
        {
            MapModule.MoveFloorItem(floor, tileIdx, Altitude.FromInt(heightHundredths));

            // MoveFloorItem only recomputes the tile it was handed when the item has not actually
            // moved, which is right for a 1x1 item and wrong for the 4x4, 6x6 and 8x8 tiles in this
            // family: the rest of their footprint would keep the old height and furniture placed
            // there would sit at it.
            RecomputeFootprint(floor);
        }

        floor.MarkDirty();

        await SendComposerToRoomAsync(floor.GetUpdateComposer()).ConfigureAwait(true);

        return true;
    }

    private void RecomputeFootprint(IRoomFloorItem floor)
    {
        if (
            !MapModule.GetTileIdForSize(
                floor.X,
                floor.Y,
                floor.Rotation,
                floor.Definition.Width,
                floor.Definition.Length,
                out List<int> tileIds
            )
        )
        {
            return;
        }

        foreach (int idx in tileIds)
        {
            MapModule.ComputeTile(idx);
        }
    }

    public async Task<RoomDimmerStateSnapshot?> GetDimmerStateAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    )
    {
        FurnitureRoomDimmerLogic? dimmer = await FindDimmerAsync(ctx, itemId).ConfigureAwait(true);

        return dimmer is null ? null : Describe(itemId, dimmer);
    }

    public async Task<RoomDimmerStateSnapshot?> ToggleDimmerAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    )
    {
        FurnitureRoomDimmerLogic? dimmer = await FindDimmerAsync(ctx, itemId).ConfigureAwait(true);

        if (dimmer is null)
        {
            return null;
        }

        await dimmer.TogglePowerAsync().ConfigureAwait(true);

        return Describe(itemId, dimmer);
    }

    public async Task<RoomDimmerStateSnapshot?> SaveDimmerPresetAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int presetNumber,
        int effectId,
        string colorHex,
        int brightness,
        bool apply,
        CancellationToken ct
    )
    {
        FurnitureRoomDimmerLogic? dimmer = await FindDimmerAsync(ctx, itemId).ConfigureAwait(true);

        if (dimmer is null)
        {
            return null;
        }

        await dimmer
            .SavePresetAsync(presetNumber, effectId, colorHex, brightness, apply)
            .ConfigureAwait(true);

        return Describe(itemId, dimmer);
    }

    private async Task<FurnitureRoomDimmerLogic?> FindDimmerAsync(
        ActionContext ctx,
        RoomObjectId itemId
    )
    {
        IRoomItem? item = await FindManipulableItemAsync(ctx, itemId).ConfigureAwait(true);

        return item?.Logic as FurnitureRoomDimmerLogic;
    }

    private static RoomDimmerStateSnapshot Describe(
        RoomObjectId itemId,
        FurnitureRoomDimmerLogic dimmer
    ) =>
        new()
        {
            ItemId = itemId,
            Presets = dimmer.GetPresets(),
            SelectedPresetId = dimmer.SelectedPresetId,
            IsOn = dimmer.IsOn,
        };

    public async Task<bool> SetPostItAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        string colorHex,
        string text,
        CancellationToken ct
    )
    {
        IRoomItem? item = await FindManipulableItemAsync(ctx, itemId).ConfigureAwait(true);

        if (item?.Logic.StuffData is not ILegacyStuffData legacy)
        {
            return false;
        }

        // One legacy string carries both, colour first: the client splits on the first space and
        // treats everything after it as the note body, newlines included.
        legacy.SetState($"{colorHex} {text}");

        await item.Logic.PersistStuffDataAsync().ConfigureAwait(true);

        return true;
    }
}
