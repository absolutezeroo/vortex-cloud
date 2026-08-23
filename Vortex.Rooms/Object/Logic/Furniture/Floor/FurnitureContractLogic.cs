using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A wired trading contract: payment, reward or trade.
/// </summary>
/// <remarks>
/// Using it opens its editor, and nothing more — the client answers by asking what the contract
/// holds, which is where the terms are read. The same two-step the chests use, for the same reason:
/// the push carries only an id.
/// <para>
/// This is not the wired dialog. A contract has an editor of its own, which is why these three ship
/// as plain furniture and why the custom-contract <em>add-on</em> is a separate furni entirely.
/// </para>
/// <para>
/// These three shipped bound to <c>furniture_basic</c>, a name shared with roughly 5 800 other
/// definitions and therefore useless to match on;
/// <c>scripts/sql/wired_contract_logic_binding.sql</c> points them at these names instead. The
/// boxes that offer and cancel transactions find the contract among the furni they were pointed at
/// by that logic name — a classname is not a key in this database.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_contract_payment")]
[RoomObjectLogic("wf_contract_reward")]
[RoomObjectLogic("wf_contract_trade")]
public class FurnitureContractLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    /// <summary>Only someone who may lay out the room may price what stands in it.</summary>
    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Controller;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new WiredOpenContractMessageComposer { ContractId = _ctx.ObjectId.Value }
            )
            .ConfigureAwait(false);
    }
}
