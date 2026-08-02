using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Crackable furniture placed in the room.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomCrackable")]
public interface IRoomCrackable : IGrainWithIntegerKey
{
    /// <summary>
    /// Lands one hit on a crackable furniture. Below its target this only advances the counters the
    /// room can see; on the hit that reaches the target the furniture is consumed and the player who
    /// landed it draws from the pool the furniture is bound to.
    /// </summary>
    public Task HitCrackableAsync(ActionContext ctx, RoomObjectId objectId, CancellationToken ct);
}
