using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    /// <summary>
    /// Lands one hit on a crackable furniture. Below its target this only advances the counters the
    /// room can see; on the hit that reaches the target the furniture is consumed and the player who
    /// landed it draws from the pool the furniture is bound to.
    /// </summary>
    public Task HitCrackableAsync(ActionContext ctx, RoomObjectId objectId, CancellationToken ct);
}
