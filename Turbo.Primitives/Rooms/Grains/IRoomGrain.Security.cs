using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    /// <summary>
    /// Resolves <paramref name="ctx"/>'s effective rights in this room (owner/explicit
    /// rights/staff capabilities). Callers outside the grain (e.g. the room-entry flow) must go
    /// through this instead of approximating from a raw owner-id comparison.
    /// </summary>
    public Task<RoomControllerType> GetControllerLevelAsync(
        ActionContext ctx,
        CancellationToken ct
    );
}
