using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Snapshots.Mapping;

namespace Vortex.Primitives.Rooms.Providers;

public interface IRoomModelProvider
{
    public RoomModelSnapshot GetModelById(int modelId);
    public Task ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// Compiles a model that is not in the reference table — the floor-plan editor's save, which
    /// hands over a plan the player just drew.
    ///
    /// The compile itself is the same one <see cref="ReloadAsync" /> runs over every stored model,
    /// exposed rather than duplicated: the base-33 heights and the <c>x</c> hole are decoded in one
    /// place, so an editor-drawn room and a shipped one cannot disagree about what a plan means.
    /// Throws for a plan that has no rows at all.
    /// </summary>
    public RoomModelSnapshot CompileCustomModel(
        int modelId,
        string name,
        string model,
        int doorX,
        int doorY,
        Rotation doorRotation
    );
}
