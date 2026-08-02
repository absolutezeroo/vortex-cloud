using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>
/// Furniture in the room that hands out a prize. The alias is kept at its original name so existing
/// activations keep resolving, but the facet covers reward boxes and the welcome gift too — they are
/// the same draw with a different trigger.
/// </summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomCrackable")]
public interface IRoomCrackable : IGrainWithIntegerKey
{
    /// <summary>
    /// Lands one hit on a crackable furniture. Below its target this only advances the counters the
    /// room can see; on the hit that reaches the target the furniture is consumed and the player who
    /// landed it draws from the pool the furniture is bound to.
    /// </summary>
    public Task HitCrackableAsync(ActionContext ctx, RoomObjectId objectId, CancellationToken ct);

    /// <summary>
    /// Claims a welcome gift. Unlike a box it is not consumed — it stays for the next player — so
    /// the once-per-player rule is what stops the clicker taking it twice.
    /// </summary>
    public Task ClaimWelcomeGiftAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        CancellationToken ct
    );
}
