using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;

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
