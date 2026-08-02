using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task HitCrackableAsync(ActionContext ctx, RoomObjectId objectId, CancellationToken ct) =>
        CrackableSystem.HitCrackableAsync(ctx, objectId, ct);
}
