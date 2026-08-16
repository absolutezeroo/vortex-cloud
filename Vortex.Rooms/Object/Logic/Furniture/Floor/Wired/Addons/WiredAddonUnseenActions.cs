using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// "WIRED Add-on: Unseen Effect" — the pile runs one effect it has not run yet, cycling through
/// them all before any repeats.
/// </summary>
/// <remarks>
/// The box has no form at all: the client class declares a code and nothing else, so being present
/// in the pile is the whole configuration. It was registered and inert, which meant the pile ran
/// every effect instead of one.
/// </remarks>
[RoomObjectLogic("wf_xtra_unseen")]
public class WiredAddonUnseenActions(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.INEDITED_ACTION;

    public override Task<bool> MutatePolicyAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        ctx.Policy.EffectMode = WiredEffectModeType.Unseen;

        return Task.FromResult(true);
    }
}
