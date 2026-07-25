using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    /// <summary>
    /// Applies a staff furni-editor edit to one placed item and broadcasts the result to the room.
    /// Vortex-specific: this has no AS3 or Habbo equivalent and no ordinary-player entry point.
    ///
    /// The caller is responsible for the <c>room.furni.edit</c> capability check and for resolving
    /// the two by-name/by-id lookups the grain cannot do itself: <paramref name="newOwnerId"/> /
    /// <paramref name="newOwnerName"/> (from the typed username) and
    /// <paramref name="newDefinition"/> (from the typed definition id). Pass null for either when
    /// its <see cref="Enums.FurniEditField"/> flag is not set.
    /// </summary>
    /// <returns>An empty string on success, otherwise a short machine-readable reason code.</returns>
    public Task<string> ApplyFurniEditAsync(
        ActionContext ctx,
        VortexApplyFurniEditMessage edit,
        PlayerId? newOwnerId,
        string? newOwnerName,
        FurnitureDefinitionSnapshot? newDefinition,
        CancellationToken ct
    );
}
