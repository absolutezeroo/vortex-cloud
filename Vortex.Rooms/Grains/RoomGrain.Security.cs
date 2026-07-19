using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<RoomControllerType> GetControllerLevelAsync(
        ActionContext ctx,
        CancellationToken ct
    ) => SecurityModule.GetControllerLevelAsync(ctx);
}
