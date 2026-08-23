using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A wired chest: the furni a room's wiring draws real value from — credits in the coin chests,
/// furniture in the others.
/// </summary>
/// <remarks>
/// Using it opens the chest's screen for whoever clicked, and nothing more: the client answers by
/// asking what the chest holds, and that request is where the contents are read. The two halves are
/// one logic because the client tells them apart by classname, not by anything the server sends.
/// <para>
/// Only someone who may decorate the room can open one. That is enforced twice on purpose — here so
/// the screen never opens for a visitor, and again when the contents are asked for, since a client
/// can send that request without ever clicking the furni.
/// </para>
/// </remarks>
// Registered under the LOGIC names, not the classnames. RoomObjectModule resolves a logic with
// Definition.LogicName -- the `logic` column, which the .nitro pack fills -- so the five classnames
// this used to declare matched nothing: every chest fell through to default_floor and clicking one
// did nothing at all. furniture_coinschest covers wf_storage_coins1/2, furniture_furnichest covers
// wf_storage_furni1/2 and wf_storage_furni_starter.
[RoomObjectLogic("furniture_coinschest")]
[RoomObjectLogic("furniture_furnichest")]
public class FurnitureWiredChestLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public override FurnitureUsageType GetUsagePolicy() => FurnitureUsageType.Controller;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new WiredChestOpenMessageComposer { ChestId = _ctx.ObjectId.Value })
            .ConfigureAwait(false);
    }
}
