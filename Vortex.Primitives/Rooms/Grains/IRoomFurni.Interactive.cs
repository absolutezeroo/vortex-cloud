using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomFurni
{
    /// <summary>
    /// Dresses a mannequin in the given look. The figure comes from the caller, not the packet —
    /// the client sends only which mannequin, and the server reads the requester's own avatar, so
    /// nobody can put an arbitrary figure on someone else's furniture.
    /// </summary>
    /// <returns>False if the item is absent, is not a mannequin, or the actor lacks room rights.</returns>
    public Task<bool> SetMannequinOutfitAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        string figure,
        string gender,
        CancellationToken ct
    );

    /// <summary>Renames a mannequin's saved outfit.</summary>
    public Task<bool> SetMannequinNameAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        string name,
        CancellationToken ct
    );

    /// <summary>
    /// Writes a sticky note's paper colour and text. Both travel together in one legacy string, so
    /// there is no way to change one without rewriting the other.
    /// </summary>
    public Task<bool> SetPostItAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        string colorHex,
        string text,
        CancellationToken ct
    );

    /// <summary>
    /// Raises or lowers a magic stack tile, which is what everything stacked on it then sits on.
    /// </summary>
    /// <param name="heightHundredths">
    /// Hundredths of a tile, as the widget sends it. <c>-100</c> is its "back to normal" button and
    /// drops the tile onto whatever is naturally under it.
    /// </param>
    /// <param name="multiWalk">
    /// The multi-walk checkbox, absent from the packet unless it is what moved.
    /// </param>
    /// <returns>False if the item is absent, is not a magic tile, the actor lacks room rights, or
    /// the requested height is out of range.</returns>
    public Task<bool> SetCustomStackHeightAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int heightHundredths,
        bool? multiWalk,
        CancellationToken ct
    );

    /// <summary>Reads a moodlight for its dialog. Null when the item is absent, is not a moodlight,
    /// or the actor lacks room rights.</summary>
    public Task<RoomDimmerStateSnapshot?> GetDimmerStateAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    );

    /// <summary>
    /// Flips a moodlight on or off and returns what the dialog should now show.
    /// </summary>
    public Task<RoomDimmerStateSnapshot?> ToggleDimmerAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    );

    /// <summary>
    /// Overwrites one of a moodlight's three presets, switching to it when <paramref name="apply"/>
    /// is set, and returns what the dialog should now show.
    /// </summary>
    public Task<RoomDimmerStateSnapshot?> SaveDimmerPresetAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int presetNumber,
        int effectId,
        string colorHex,
        int brightness,
        bool apply,
        CancellationToken ct
    );
}
