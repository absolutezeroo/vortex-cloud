using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

public abstract class FurnitureWiredAddonLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredLogic(grainFactory, stuffDataFactory, ctx), IWiredAddon
{
    public override WiredType WiredType => WiredType.Addon;

    public virtual Task<bool> MutatePolicyAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    ) => Task.FromResult(true);

    public virtual Task BeforeEffectsAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.CompletedTask;

    public virtual Task AfterEffectsAsync(IWiredProcessingContext ctx, CancellationToken ct) =>
        Task.CompletedTask;
}
