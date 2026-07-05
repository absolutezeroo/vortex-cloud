using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<RoomControllerType> GetControllerLevelAsync(
        ActionContext ctx,
        CancellationToken ct
    ) => SecurityModule.GetControllerLevelAsync(ctx);
}
