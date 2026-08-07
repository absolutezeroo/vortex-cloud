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

    /// <summary>
    /// Walks the actor through a one-way gate: onto the tile behind it, facing the way they came
    /// out. Does nothing unless they are standing on the gate's own tile and the tile behind it can
    /// be walked onto.
    /// </summary>
    /// <returns>False if the item is absent, is not a gate, the actor is not on it, or the far side
    /// is blocked.</returns>
    public Task<bool> EnterOneWayDoorAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    );

    /// <summary>
    /// Tints the room from a background toner. Room rights, not ownership: the toner colours
    /// everyone's view, so it belongs to whoever may build here.
    /// </summary>
    public Task<bool> SetBackgroundColorAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int hue,
        int saturation,
        int lightness,
        CancellationToken ct
    );

    /// <summary>
    /// Cashes in a credit furni and consumes it. Returns how many credits it was worth, for the
    /// caller to pay out, or zero when the item is absent, is not the actor's own, or names no
    /// value.
    /// </summary>
    public Task<int> RedeemCreditFurniAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        CancellationToken ct
    );

    /// <summary>
    /// Unwraps a present and consumes it. Returns what was inside, for the caller to grant, or null
    /// when the item is absent, is not a present, is not the actor's own, or holds nothing the
    /// server can resolve.
    /// </summary>
    /// <remarks>
    /// Consuming and granting are split across two grains on purpose, in that order: the wrapping is
    /// gone before anything is handed out, so a failure downstream costs the player their gift once
    /// rather than letting a repeated click on a present that is still there mint copies of it.
    /// </remarks>
    public Task<PresentContentsSnapshot?> OpenPresentAsync(
        ActionContext ctx,
        RoomObjectId itemId,
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
