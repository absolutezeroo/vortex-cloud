using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Object;

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
}
